[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

if ($env:OS -ne 'Windows_NT') {
    throw 'This probe must be run on Windows.'
}

$probeRoot = Join-Path $env:TEMP ("FlashLock-AclProbe-" + [guid]::NewGuid().ToString('N'))
$sample = Join-Path $probeRoot 'sample.txt'
$aclBackup = Join-Path $env:TEMP ("FlashLock-AclBackup-" + [guid]::NewGuid().ToString('N') + '.txt')

Write-Host "Creating disposable probe at: $probeRoot"
New-Item -ItemType Directory -Path $probeRoot | Out-Null
Set-Content -Path $sample -Value 'FlashLock ACL probe'

try {
    $driveLetter = [System.IO.Path]::GetPathRoot($probeRoot).Substring(0, 1)
    $drive = Get-PSDrive -Name $driveLetter
    if ($drive.Provider.Name -ne 'FileSystem') {
        throw 'Probe path is not on a filesystem drive.'
    }

    & icacls $probeRoot /save $aclBackup /t /c | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Failed to back up the probe ACL.' }

    # This experiment is deliberately limited to the disposable temp directory.
    # SYSTEM and Administrators retain full control; Everyone receives read/execute.
    & icacls $probeRoot /inheritance:r | Out-Null
    & icacls $probeRoot /grant:r '*S-1-5-18:(OI)(CI)F' '*S-1-5-32-544:(OI)(CI)F' '*S-1-1-0:(OI)(CI)RX' | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Failed to apply the probe ACL.' }

    Write-Host ''
    Write-Host 'Protection ACL applied to disposable folder.' -ForegroundColor Green
    Write-Host 'Manual checks to perform from a NON-ELEVATED Explorer window:'
    Write-Host "  1. Open $sample (should work)"
    Write-Host "  2. Copy $sample somewhere else (should work)"
    Write-Host "  3. Try to edit, rename, or delete it (should fail)"
    Write-Host "  4. Try to create a file inside $probeRoot (should fail)"
    Write-Host ''
    Read-Host 'Press Enter when finished; the script will delete the disposable probe'
}
finally {
    # Elevated/admin cleanup can remove the test directory even if the normal token cannot.
    try {
        & takeown /f $probeRoot /r /d y | Out-Null
        & icacls $probeRoot /grant:r "${env:USERNAME}:(OI)(CI)F" /t /c | Out-Null
    } catch { }

    Remove-Item -LiteralPath $probeRoot -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $aclBackup -Force -ErrorAction SilentlyContinue
    Write-Host 'Probe cleaned up.'
}
