# Recovery Guide

FlashLock prioritizes recoverability over tamper resistance. Administrators retain Full Control in the protected ACL profile.

## Normal recovery

If the app displays **RECOVERY NEEDED**:

1. Keep the USB connected.
2. Run `FlashLock.exe` from that same USB.
3. Select **Recover**.
4. Enter the owner PIN.
5. Approve UAC.

FlashLock will read `.flashlock\acl-snapshot.jsonl` and restore the saved user-data DACLs.

## Emergency script

If the UI/helper cannot run but the snapshot still exists, open **PowerShell as Administrator** and run the repository copy of:

```powershell
.\tools\Manual-Recover-Acl.ps1 -DriveRoot E:\
```

The script:

- requires Administrator rights
- refuses the Windows system drive
- restores only the Access/DACL portion saved by FlashLock
- skips files that no longer exist

It does not require the owner PIN because a local Administrator is already outside FlashLock's v0.1 security boundary and must retain a repair path.

## Snapshot missing

If the drive is protected but `.flashlock\acl-snapshot.jsonl` is missing, stop making changes and do not improvise with blanket ACL resets on valuable data. Work from a backup or manually inspect the current ACLs first.

FlashLock deliberately does not auto-reset unknown ACLs because doing so could destroy pre-existing custom permissions.
