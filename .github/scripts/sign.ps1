<#
.SYNOPSIS
    Authenticode-signs the given files if a code-signing certificate is configured.

.DESCRIPTION
    Reads the certificate from two repository secrets, passed in as environment variables:
      SIGNING_CERT_PFX_BASE64   base64 of the .pfx file
      SIGNING_CERT_PASSWORD     its password
    With no certificate configured this prints a notice and exits successfully, so releases
    keep working unsigned. Provenance attestation (see release.yml) is independent of this.
#>
param([Parameter(Mandatory)] [string[]] $Files)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrEmpty($env:SIGNING_CERT_PFX_BASE64)) {
    Write-Host '::notice::No code-signing certificate configured; skipping Authenticode signing.'
    exit 0
}

$signtool = Get-ChildItem "${env:ProgramFiles(x86)}\Windows Kits\10\bin\*\x64\signtool.exe" |
    Sort-Object FullName -Descending | Select-Object -First 1 -ExpandProperty FullName
if (-not $signtool) { throw 'signtool.exe not found (Windows SDK missing on this runner).' }

$pfx = Join-Path $env:RUNNER_TEMP 'signing.pfx'
try {
    [IO.File]::WriteAllBytes($pfx, [Convert]::FromBase64String($env:SIGNING_CERT_PFX_BASE64))

    foreach ($file in $Files) {
        & $signtool sign /fd SHA256 /td SHA256 /tr 'http://timestamp.digicert.com' `
            /f $pfx /p $env:SIGNING_CERT_PASSWORD /d 'QweNumpad' `
            /du 'https://github.com/Muratmd/QweNumpad' $file
        if ($LASTEXITCODE -ne 0) { throw "signtool failed for $file" }
    }
}
finally {
    Remove-Item $pfx -Force -ErrorAction SilentlyContinue
}
