# Feasibility Notes

## What Windows gives us

Windows supports disk-level read-only attributes and NTFS file/directory ACLs.

The disk-level option is useful for same-machine hardening, but it is not the foundation of FlashLock's portable promise. The Windows disk attribute can persist across reboots on a computer, yet that does not mean a different computer will honor the prior machine's state.

## Portable mechanism chosen for v1

NTFS stores security descriptors with files/directories. FlashLock can therefore apply a root protection DACL that travels with the filesystem to another Windows computer.

Conceptually:

- `SYSTEM` — Full Control
- `BUILTIN\Administrators` — Full Control
- `Everyone` — Read & Execute

Inheritance propagates the profile to children. Normal unelevated use can read but not request write/delete rights. Elevated administrators retain recovery/control capability.

## Why NTFS is required

Windows ACL APIs operate on NTFS file/directory security descriptors. FAT32 and exFAT do not provide the same ACL model. FlashLock must detect the filesystem and refuse portable protection on unsupported filesystems rather than pretend protection exists.

## Critical experiments before root-drive mutation

1. Apply the planned ACL to a disposable NTFS folder.
2. Confirm read/copy-out succeeds and write/delete/rename fails from a normal token.
3. Confirm elevated recovery succeeds.
4. Repeat on two Windows 11 machines.
5. Repeat using at least two USB vendors/controllers.
6. Validate root ACL backup/restore on a dedicated empty test USB.
7. Confirm unexpected unplug during protection transition does not leave an unrecoverable state.

Only after those experiments pass should FL-004 enable drive-root mutation.
