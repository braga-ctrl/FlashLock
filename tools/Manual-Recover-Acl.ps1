[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory)]
    [ValidatePattern('^[A-Za-z]:\\?$')]
    [string]$DriveRoot
)

$ErrorActionPreference = 'Stop'
if ($env:OS -ne 'Windows_NT') { throw 'Windows is required.' }

$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = [Security.Principal.WindowsPrincipal]::new($identity)
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw 'Run this emergency recovery script from an elevated PowerShell window.'
}

$DriveRoot = [IO.Path]::GetPathRoot($DriveRoot)
$systemRoot = [IO.Path]::GetPathRoot($env:SystemRoot)
if ($DriveRoot -ieq $systemRoot) { throw 'Refusing to target the Windows system drive.' }

$snapshot = Join-Path $DriveRoot '.flashlock\acl-snapshot.jsonl'
if (-not (Test-Path $snapshot)) { throw "Snapshot not found: $snapshot" }

$records = Get-Content $snapshot | Where-Object { $_.Trim() } | ForEach-Object { $_ | ConvertFrom-Json }
$records = $records | Sort-Object @{ Expression = { ($_.relativePath -split '[\\/]').Count }; Descending = $true }

foreach ($record in $records) {
    $path = if ($record.relativePath -eq '.') { $DriveRoot } else { Join-Path $DriveRoot $record.relativePath }
    if (-not (Test-Path -LiteralPath $path)) { continue }
    if ($PSCmdlet.ShouldProcess($path, 'Restore saved DACL')) {
        $acl = Get-Acl -LiteralPath $path
        $acl.SetSecurityDescriptorSddlForm($record.sddl, [Security.AccessControl.AccessControlSections]::Access)
        Set-Acl -LiteralPath $path -AclObject $acl
    }
}

Write-Host 'Saved DACLs restored. Open FlashLock and verify the drive reports UNLOCKED/RECOVERY as appropriate.' -ForegroundColor Green
