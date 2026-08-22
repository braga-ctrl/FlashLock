[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

if ($env:OS -ne 'Windows_NT') {
    throw 'This probe must be run on Windows.'
}

$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = [Security.Principal.WindowsPrincipal]::new($identity)
$isAdmin = $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host 'Re-launching the disposable ACL probe with administrator rights...'
    $args = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', ('"' + $PSCommandPath + '"'))
    Start-Process powershell.exe -Verb RunAs -ArgumentList ($args -join ' ') -Wait
    exit
}

$probeRoot = Join-Path $env:TEMP ("FlashLock-AclProbe-" + [guid]::NewGuid().ToString('N'))
$sample = Join-Path $probeRoot 'sample.txt'

Write-Host "Creating disposable probe at: $probeRoot"
New-Item -ItemType Directory -Path $probeRoot | Out-Null
Set-Content -Path $sample -Value 'FlashLock ACL probe'

try {
    & icacls $probeRoot /inheritance:r | Out-Null
    & icacls $probeRoot /grant:r '*S-1-5-18:(OI)(CI)F' '*S-1-5-32-544:(OI)(CI)F' '*S-1-1-0:(OI)(CI)RX' | Out-Null
    & icacls $sample /inheritance:r | Out-Null
    & icacls $sample /grant:r '*S-1-5-18:F' '*S-1-5-32-544:F' '*S-1-1-0:RX' | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Failed to apply the probe ACL.' }

    Write-Host ''
    Write-Host 'Protection ACL applied to disposable folder.' -ForegroundColor Green
    Write-Host 'Use a NORMAL (non-elevated) Explorer window for these checks:'
    Write-Host "  1. Open $sample (should work)"
    Write-Host "  2. Copy $sample somewhere else (should work)"
    Write-Host '  3. Try to edit, rename, or delete it (should fail)'
    Write-Host "  4. Try to create a file inside $probeRoot (should fail)"
    Write-Host ''
    Read-Host 'Press Enter after testing; this elevated script will clean up the probe'
}
finally {
    try {
        & takeown /f $probeRoot /r /d y | Out-Null
        & icacls $probeRoot /grant:r "${env:USERNAME}:(OI)(CI)F" /t /c | Out-Null
    } catch { }

    Remove-Item -LiteralPath $probeRoot -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host 'Probe cleaned up.'
}
