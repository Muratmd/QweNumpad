<#
.SYNOPSIS
    Browser detection test for QweNumpad.

.DESCRIPTION
    Opens tools/browser-test-page.html in the default browser, then injects real
    keystrokes into four fields and reads the results back through UI Automation.

    This is the test that covers the riskiest claim in the design: that automatic
    detection works for web inputs. It checks that
      * input type=number       -> detected numeric (RangeValue / Spinner pattern)
      * text input named phone  -> detected numeric (keyword heuristic)
      * a plain text input      -> left alone
      * input type=password     -> left alone

    Run from a normal (non-elevated) PowerShell, with QweNumpad not already running.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File tools\browser-test.ps1
#>
[CmdletBinding()]
param(
    [string]$Configuration = 'Debug'
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$exePath = Join-Path $repoRoot "src\QweNumpad.App\bin\$Configuration\net8.0-windows\QweNumpad.exe"
$pagePath = Join-Path $PSScriptRoot 'browser-test-page.html'

Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes

Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;

public static class Keys2
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
        public int padding1;
        public int padding2;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

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
        System.Threading.Thread.Sleep(25);
        Send(vk, true);
        System.Threading.Thread.Sleep(25);
    }
}
'@

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

function Find-ById {
    param($Root, [string] $Id, [int] $TimeoutSeconds = 20)

    $condition = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::AutomationIdProperty, $Id)

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        $found = $Root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condition)
        if ($found) { return $found }
        Start-Sleep -Milliseconds 400
    }

    throw "Sayfa alani bulunamadi: '$Id' (tarayici erisilebilirlik agacini acmamis olabilir)"
}

function Clear-And-Type {
    param($Element, [uint16[]] $VirtualKeys)

    $value = $Element.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern)
    $value.SetValue('')
    $Element.SetFocus()
    Start-Sleep -Milliseconds 900   # give the focus watcher time to classify

    foreach ($vk in $VirtualKeys) { [Keys2]::Tap($vk) }
    Start-Sleep -Milliseconds 400

    return $value.Current.Value
}

if (-not (Test-Path $exePath)) { throw "Derlenmis exe bulunamadi: $exePath" }
if (-not (Test-Path $pagePath)) { throw "Test sayfasi bulunamadi: $pagePath" }
if (Get-Process -Name 'QweNumpad' -ErrorAction SilentlyContinue) {
    throw 'QweNumpad zaten calisiyor. Testten once kapat.'
}

Write-Host "QweNumpad baslatiliyor"
$app = Start-Process -FilePath $exePath -PassThru
Start-Sleep -Seconds 2

Write-Host "Test sayfasi aciliyor: $pagePath"
Start-Process $pagePath
Start-Sleep -Seconds 6

try {
    $root = [System.Windows.Automation.AutomationElement]::RootElement

    $qwe = [uint16[]](0x51, 0x57, 0x45)

    $numField = Find-ById -Root $root -Id 'numField'
    Assert-Equal 'input type=number' '789' (Clear-And-Type -Element $numField -VirtualKeys $qwe)

    $telField = Find-ById -Root $root -Id 'telField'
    Assert-Equal 'Telefon Numarasi (text)' '789' (Clear-And-Type -Element $telField -VirtualKeys $qwe)

    $txtField = Find-ById -Root $root -Id 'txtField'
    Assert-Equal 'Aciklama (text)' 'qwe' (Clear-And-Type -Element $txtField -VirtualKeys $qwe)

    # The password field is on the page for manual inspection only: browsers refuse to
    # expose a password value through UI Automation, so it cannot be asserted here.
    # That behaviour is covered by the Core unit test Password_fields_are_never_remapped.
}
finally {
    if ($app -and -not $app.HasExited) {
        $app.Kill()
        $app.WaitForExit(5000)
    }
}

Write-Host ''
if ($failures.Count -gt 0) {
    Write-Host ("BROWSER TEST BASARISIZ ({0} hata)" -f $failures.Count) -ForegroundColor Red
    exit 1
}

Write-Host 'BROWSER TEST BASARILI' -ForegroundColor Green
exit 0
