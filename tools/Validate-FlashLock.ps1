[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet('Prepare','VerifyProtected','VerifyUnlocked','Cleanup')]
    [string]$Mode,

    [Parameter(Mandatory)]
    [ValidatePattern('^[A-Za-z]:\\?$')]
    [string]$DriveRoot
)

$ErrorActionPreference = 'Stop'
$DriveRoot = ([IO.Path]::GetPathRoot($DriveRoot))
$folder = Join-Path $DriveRoot 'FlashLock-Validation'
$sample = Join-Path $folder 'do-not-delete.txt'
$tempCopy = Join-Path $env:TEMP ('FlashLock-copy-' + [guid]::NewGuid().ToString('N') + '.txt')

function Expect-Failure([scriptblock]$Action, [string]$Name) {
    try {
        & $Action
        throw "FAIL: $Name unexpectedly succeeded."
    } catch {
        if ($_.Exception.Message -like 'FAIL:*') { throw }
        Write-Host "PASS: $Name was blocked." -ForegroundColor Green
    }
}

switch ($Mode) {
    'Prepare' {
        New-Item -ItemType Directory -Path $folder -Force | Out-Null
        Set-Content -Path $sample -Value "FlashLock validation file $(Get-Date -Format o)"
        Write-Host "Prepared $sample. Now protect the USB with FlashLock." -ForegroundColor Green
    }
    'VerifyProtected' {
        if (-not (Test-Path $sample)) { throw 'Validation sample is missing. Run Prepare while unlocked first.' }
        Get-Content $sample | Out-Null
        Copy-Item $sample $tempCopy -Force
        Remove-Item $tempCopy -Force
        Write-Host 'PASS: read and copy-out work.' -ForegroundColor Green
        Expect-Failure { Set-Content -Path $sample -Value 'overwrite attempt' } 'overwrite existing file'
        Expect-Failure { Rename-Item -Path $sample -NewName 'renamed.txt' } 'rename file'
        Expect-Failure { Remove-Item -Path $sample -Force } 'delete file'
        Expect-Failure { Set-Content -Path (Join-Path $folder 'new-file.txt') -Value 'write attempt' } 'create new file'
        Expect-Failure { New-Item -ItemType Directory -Path (Join-Path $folder 'new-folder') | Out-Null } 'create new folder'
        Write-Host 'Protected behavior passed. Repeat this mode on the second Windows PC.' -ForegroundColor Cyan
    }
    'VerifyUnlocked' {
        if (-not (Test-Path $sample)) { throw 'Validation sample is missing.' }
        Add-Content -Path $sample -Value 'Unlocked write succeeded.'
        $renamed = Join-Path $folder 'renamed.txt'
        Rename-Item -Path $sample -NewName 'renamed.txt'
        Rename-Item -Path $renamed -NewName 'do-not-delete.txt'
        Set-Content -Path (Join-Path $folder 'new-file.txt') -Value 'Unlocked create succeeded.'
        Remove-Item (Join-Path $folder 'new-file.txt') -Force
        Write-Host 'PASS: normal write/rename/delete operations work while unlocked.' -ForegroundColor Green
    }
    'Cleanup' {
        Remove-Item -LiteralPath $folder -Recurse -Force
        Write-Host 'Validation folder removed.' -ForegroundColor Green
    }
}
