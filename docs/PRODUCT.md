# Product Definition

## One sentence

FlashLock is a portable Windows app stored on a USB flash drive that lets the owner switch the drive between **protected read-only-for-normal-use** and **normal read/write** modes.

## Primary user story

> Before I lend my USB drive to someone, I run FlashLock and turn protection on. They can open and copy my files, but normal Windows apps cannot delete, rename, edit, overwrite, or add content. When I get the drive back, I run FlashLock, enter my owner PIN, approve elevation, and unlock it.

## MVP behavior

### Protected

- Existing files remain readable.
- Existing executables may run if Windows policy permits.
- Copying files **out** of the USB is allowed.
- Creating files/folders is blocked for normal users.
- Modifying/overwriting files is blocked.
- Renaming and deleting files/folders is blocked.

### Unlocked

- Normal NTFS read/write behavior is restored.
- Original ACL state is restored rather than guessed.

## Non-goals for v1

- Hardware-grade write blocking.
- Preventing a local Windows Administrator from deliberately bypassing protection.
- macOS or Linux support.
- FAT32/exFAT portable protection.
- Encryption or confidentiality. Borrowers are intentionally allowed to read the files.
- Automatic formatting or repartitioning of user drives.

## UX principles

1. **Drive-local:** FlashLock protects the volume it is running from, never an arbitrary selected disk by default.
2. **Safe failure:** if identity or filesystem checks are ambiguous, protection changes are refused.
3. **Reversible:** capture original ACL state before applying a protection profile.
4. **Visible state:** clearly show Protected, Unlocked, Unsupported, or Recovery Required.
5. **No Internet dependency:** owner PIN verification and protection work offline.
