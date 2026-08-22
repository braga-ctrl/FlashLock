# v0.1 Validation Matrix

FlashLock should not be called hardware-validated until this matrix passes on a dedicated NTFS test USB.

## 1. Windows build gate

```powershell
dotnet restore FlashLock.sln
dotnet build FlashLock.sln -c Release
dotnet test FlashLock.sln -c Release
```

Expected: zero build errors and all tests pass.

## 2. Disposable-folder ACL probe

```powershell
.\tools\Test-AclProtection.ps1
```

From a normal, non-elevated Explorer window verify:

- open/read succeeds
- copy-out succeeds
- edit/overwrite fails
- rename fails
- delete fails
- create file/folder fails

The elevated probe must clean up its temporary folder afterward.

## 3. Dedicated USB preparation

Use a USB containing **no irreplaceable data**.

- filesystem: NTFS
- copy the portable release contents to the USB root
- run `FlashLock.exe` from the USB
- confirm the UI shows the expected label/root/volume serial
- confirm FlashLock refuses the same binaries if copied to the Windows system drive

Prepare a validation file while unlocked:

```powershell
.\tools\Validate-FlashLock.ps1 -Mode Prepare -DriveRoot E:\
```

## 4. Protect

In FlashLock:

1. Choose **Set up & protect**.
2. Create the owner PIN/passphrase.
3. Approve UAC.
4. Confirm the app reports `PROTECTED` only after the operation completes.

Then:

```powershell
.\tools\Validate-FlashLock.ps1 -Mode VerifyProtected -DriveRoot E:\
```

Expected: read/copy-out pass; overwrite/rename/delete/create are all blocked.

## 5. Unplug/replug same PC

Safely eject, unplug, reconnect, then rerun `VerifyProtected` before opening FlashLock.

Expected: protection remains effective because the DACLs live on NTFS.

## 6. Second Windows PC — required portable proof

Move the still-protected USB to a second Windows 10/11 PC using a normal non-administrator account/session and run:

```powershell
.\tools\Validate-FlashLock.ps1 -Mode VerifyProtected -DriveRoot <letter>:\
```

Expected: same protected behavior without installing FlashLock on that PC.

This is the key validation that cannot be simulated by CI.

## 7. Unlock and exact restore

Move the USB to the owner PC or run FlashLock on the second PC, choose **Unlock drive**, enter the PIN, approve UAC, then run:

```powershell
.\tools\Validate-FlashLock.ps1 -Mode VerifyUnlocked -DriveRoot E:\
```

Expected: write/create/rename/delete work again.

## 8. Recovery test

On a disposable test USB, deliberately interrupt a protection operation only after keeping a separate backup of the test data. Confirm the next launch exposes Recovery Required and that either the UI Recovery action or:

```powershell
.\tools\Manual-Recover-Acl.ps1 -DriveRoot E:\
```

restores normal permissions.

## 9. Repeatability

Minimum evidence before calling v0.1 hardware-validated:

- two Windows PCs
- two different USB devices/controllers
- protect/unplug/replug/unlock cycle repeated twice per device
- no data-loss or unrecoverable ACL state

Record OS versions, USB model/filesystem, and result in the corresponding GitHub issue.
