<#
.SYNOPSIS
    Compiles installer\QweNumpad.iss with Inno Setup. Expects publish\QweNumpad.exe to exist.
#>
param([Parameter(Mandatory)] [string] $Version)

$ErrorActionPreference = 'Stop'

$iscc = Get-Command iscc.exe -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source
if (-not $iscc) {
    $candidate = "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
    if (-not (Test-Path $candidate)) {
        Write-Host 'Inno Setup not found, installing via Chocolatey'
        choco install innosetup --no-progress -y | Out-Host
    }
    $iscc = $candidate
}

$numeric = ($Version -split '-', 2)[0]
& $iscc "/DAppVersion=$Version" "/DAppNumericVersion=$numeric" installer\QweNumpad.iss
if ($LASTEXITCODE -ne 0) { throw "ISCC failed with exit code $LASTEXITCODE" }

Get-ChildItem installer\Output\*.exe | ForEach-Object { Write-Host "Built $($_.Name) ($([math]::Round($_.Length / 1MB, 1)) MB)" }
