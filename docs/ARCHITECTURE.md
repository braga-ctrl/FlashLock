# Architecture

## Components

### FlashLock.App

Portable WPF UI running with the normal user token.

Responsibilities:

- identify the volume containing the running executable
- display filesystem, capacity, volume serial, compatibility and state
- first-run owner PIN capture and PIN entry
- launch the privileged helper through UAC
- exchange one request/response over a random named pipe
- surface Protected / Unlocked / Unsupported / Recovery Required states

No PIN is placed on the process command line.

### FlashLock.Core

Shared domain/security implementation:

- volume identity and safety guardrails
- NTFS compatibility checks
- PBKDF2 PIN verification and temporary failed-attempt lockout
- configuration state machine
- ACL snapshot capture / exact DACL restoration
- protected ACL application and read-back verification
- automatic rollback on failed protection

### FlashLock.Elevated

Minimal console helper whose manifest requires Administrator privileges.

Responsibilities:

- receive the request from the already-created named pipe
- independently verify that its own executable resides on the requested target drive
- reject the Windows system volume and non-NTFS volumes
- re-check the expected volume serial
- run Protect / Unlock / Recover through `ProtectionEngine`
- return only status/result data to the UI

## State machine

```text
NOT_CONFIGURED
    |
    | create PIN + Protect
    v
APPLYING
    |------------------------.
    | verified               | exception / interrupted
    v                        v
PROTECTED             RECOVERY_REQUIRED
    |                        |
    | Unlock + PIN           | Recover + PIN
    v                        |
RESTORING <------------------'
    |------------------------.
    | exact restore verified | failure
    v                        v
UNLOCKED               RECOVERY_REQUIRED
    |
    | Protect + PIN
    '-----------------------> APPLYING
```

## Target identity

The target is never chosen from a user-entered arbitrary path. The UI derives the root from `FlashLock.exe`, and the elevated helper repeats the same check against `FlashLock.Elevated.exe`.

Operations are also bound to the NTFS volume serial number captured by `GetVolumeInformationW`. A drive-letter change does not invalidate the volume identity; a different/reformatted volume does.

## Snapshot and mutation algorithm

Protection is intentionally transactional in spirit:

1. Validate target and PIN/state.
2. Persist state `Applying`.
3. Enumerate user files/directories while refusing reparse points.
4. Capture the **Access** portion of every DACL as SDDL into `.flashlock\acl-snapshot.jsonl`.
5. Flush and atomically publish the completed snapshot.
6. Apply the protected allow-list ACL deepest-first, root last.
7. Apply protected ACLs to FlashLock metadata.
8. Read back every protected user ACL and verify no unexpected principal/write capability remains.
9. Persist state `Protected`.

If steps 4-8 fail, FlashLock attempts an immediate restore from the snapshot. If rollback itself fails, state becomes `RecoveryRequired` and the snapshot remains available.

## Protected ACL profile

Each protected object has ACL inheritance disabled and explicit allow rules only:

- `SYSTEM`: Full Control
- `BUILTIN\Administrators`: Full Control
- `Everyone`: Read & Execute

There are no explicit Deny ACEs. This makes recovery more predictable and lets the elevated helper retain control.

## Unlock / recovery

1. Validate target and owner PIN.
2. Persist state `Restoring`.
3. Load the snapshot.
4. Restore DACLs deepest-first and the root last.
5. Read back each DACL and compare it to its saved SDDL.
6. Persist state `Unlocked`.
7. Delete the now-obsolete snapshot on best effort.

The `.flashlock` metadata directory intentionally retains its restricted app-owned ACL after unlock so a normal borrower cannot edit the PIN verifier or state file. User data ACLs are restored exactly.

## Recovery independence

`tools/Manual-Recover-Acl.ps1` provides an Administrator-only escape hatch that reads the same JSONL snapshot and restores saved DACLs without depending on the WPF application or helper binary.
