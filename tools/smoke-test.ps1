<#
.SYNOPSIS
    End-to-end smoke test for QweNumpad.

.DESCRIPTION
    Launches the app with its diagnostics window, focuses the two test fields inside it,
    injects real keystrokes with SendInput, and reads the resulting text back through
    UI Automation.

    Asserts that:
      * a field detected as numeric turns q w e a s d z x c space into 7 8 9 4 5 6 1 2 3 0
      * a field detected as text is left alone (q w e stays "qwe")
      * Shift bypasses the mapping even in a numeric field (Shift+q stays "Q")

    Run from a normal (non-elevated) PowerShell. The app must not already be running.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File tools\smoke-test.ps1
#>
[CmdletBinding()]
param(
    [string]$Configuration = 'Debug'
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$exePath = Join-Path $repoRoot "src\QweNumpad.App\bin\$Configuration\net8.0-windows\QweNumpad.exe"

Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes

Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;

public static class Keyboard
{
    [StructLayout(LayoutKind.Sequential)]
    public struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct INPUT
    {
        public uint type;
        public KEYBDINPUT ki;
        // Pad to the size of the largest union member (MOUSEINPUT) on 64-bit.
        public int padding1;
        public int padding2;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    private const uint KEYEVENTF_KEYUP = 0x0002;

    private static void Send(ushort vk, bool up)
    {
        var input = new INPUT
        {
            type = 1,
            ki = new KEYBDINPUT { wVk = vk, wScan = 0, dwFlags = up ? KEYEVENTF_KEYUP : 0 }
        };

        if (SendInput(1, new[] { input }, Marshal.SizeOf(typeof(INPUT))) == 0)
            throw new InvalidOperationException("SendInput failed: " + Marshal.GetLastWin32Error());
    }

    public static void Tap(ushort vk)
    {
        Send(vk, false);
        System.Threading.Thread.Sleep(18);
        Send(vk, true);
        System.Threading.Thread.Sleep(18);
    }

    public static void TapWithShift(ushort vk)
    {
        const ushort VK_SHIFT = 0x10;
        Send(VK_SHIFT, false);
        System.Threading.Thread.Sleep(10);
        Tap(vk);
        Send(VK_SHIFT, true);
        System.Threading.Thread.Sleep(10);
    }
}
'@

# Looked up by AutomationId, not Name: the ids are ASCII, so this script stays
# encoding-independent across PowerShell 5.1 and 7.
function Find-Descendant {
    param(
        [Parameter(Mandatory)] $Root,
        [Parameter(Mandatory)] [string] $AutomationId,
        [int] $TimeoutSeconds = 10
    )

    $condition = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::AutomationIdProperty, $AutomationId)

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        $found = $Root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condition)
        if ($found) { return $found }
        Start-Sleep -Milliseconds 250
    }

    throw "UI Automation element not found: '$AutomationId'"
}

function Get-FieldValue {
    param([Parameter(Mandatory)] $Element)

    $pattern = $Element.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern)
    return $pattern.Current.Value
}

function Set-FieldValue {
    param([Parameter(Mandatory)] $Element, [string] $Value = '')

    $pattern = $Element.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern)
    $pattern.SetValue($Value)
}

$failures = New-Object System.Collections.Generic.List[string]

function Assert-Equal {
    param([string] $What, [string] $Expected, [string] $Actual)

    if ($Expected -ceq $Actual) {
        Write-Host ("  PASS  {0}: '{1}'" -f $What, $Actual) -ForegroundColor Green
    }
    else {
        Write-Host ("  FAIL  {0}: beklenen '{1}', gelen '{2}'" -f $What, $Expected, $Actual) -ForegroundColor Red
        $failures.Add($What)
    }
}

if (-not (Test-Path $exePath)) {
    throw "Derlenmis exe bulunamadi: $exePath`nOnce 'dotnet build' calistir."
}

if (Get-Process -Name 'QweNumpad' -ErrorAction SilentlyContinue) {
    throw 'QweNumpad zaten calisiyor. Testten once kapat.'
}

Write-Host "QweNumpad baslatiliyor: $exePath"
$process = Start-Process -FilePath $exePath -ArgumentList '--diagnostics' -PassThru

try {
    Start-Sleep -Seconds 3

    $root = [System.Windows.Automation.AutomationElement]::RootElement
    $windowCondition = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ProcessIdProperty, $process.Id)

    $deadline = (Get-Date).AddSeconds(15)
    $window = $null
    while (-not $window -and (Get-Date) -lt $deadline) {
        $window = $root.FindFirst([System.Windows.Automation.TreeScope]::Children, $windowCondition)
        if (-not $window) { Start-Sleep -Milliseconds 300 }
    }
    if (-not $window) { throw 'Tanilama penceresi bulunamadi.' }

    [void][Keyboard]::SetForegroundWindow([IntPtr]$window.Current.NativeWindowHandle)
    Start-Sleep -Milliseconds 500

    $numericField = Find-Descendant -Root $window -AutomationId 'testNumericField'
    $textField = Find-Descendant -Root $window -AutomationId 'testTextField'

    # --- 1. numeric field: the whole 3x3 grid plus space ---
    Set-FieldValue -Element $numericField -Value ''
    $numericField.SetFocus()
    Start-Sleep -Milliseconds 700   # let the focus watcher classify the field

    foreach ($vk in 0x51, 0x57, 0x45, 0x41, 0x53, 0x44, 0x5A, 0x58, 0x43, 0x20) {
        [Keyboard]::Tap([uint16]$vk)
    }
    Start-Sleep -Milliseconds 300
    Assert-Equal 'Sayisal alanda qweasdzxc+space' '7894561230' (Get-FieldValue -Element $numericField)

    # --- 2. Shift bypasses the mapping ---
    Set-FieldValue -Element $numericField -Value ''
    $numericField.SetFocus()
    Start-Sleep -Milliseconds 400
    [Keyboard]::TapWithShift([uint16]0x51)
    Start-Sleep -Milliseconds 300
    Assert-Equal 'Sayisal alanda Shift+q' 'Q' (Get-FieldValue -Element $numericField)

    # --- 3. text field is untouched ---
    Set-FieldValue -Element $textField -Value ''
    $textField.SetFocus()
    Start-Sleep -Milliseconds 700
    foreach ($vk in 0x51, 0x57, 0x45) { [Keyboard]::Tap([uint16]$vk) }
    Start-Sleep -Milliseconds 300
    Assert-Equal 'Metin alaninda qwe' 'qwe' (Get-FieldValue -Element $textField)

    # --- 4. Diagnostics "copy" reports the field, not the copy button itself ---
    # Focusing the button first is the point: that is what a real click does, and the
    # diagnostics view must keep describing the field the user was investigating.
    $numericField.SetFocus()
    Start-Sleep -Milliseconds 700
    $copyButton = Find-Descendant -Root $window -AutomationId 'copyDiagnostics'
    $copyButton.SetFocus()
    Start-Sleep -Milliseconds 900
    $copyButton.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
    Start-Sleep -Milliseconds 400
    $clip = (Get-Clipboard -Raw)
    $describesField = [bool]($clip -match 'AutomationId\s+:\s+testNumericField')
    $describesButton = [bool]($clip -match 'copyDiagnostics')
    Assert-Equal 'Tanilama kopyasi test alanini anlatiyor' 'True' "$describesField"
    Assert-Equal 'Tanilama kopyasi butonu anlatmiyor' 'False' "$describesButton"
}
finally {
    if ($process -and -not $process.HasExited) {
        $process.Kill()
        $process.WaitForExit(5000)
    }
}

Write-Host ''
if ($failures.Count -gt 0) {
    Write-Host ("SMOKE TEST BASARISIZ ({0} hata)" -f $failures.Count) -ForegroundColor Red
    exit 1
}

Write-Host 'SMOKE TEST BASARILI' -ForegroundColor Green
exit 0
