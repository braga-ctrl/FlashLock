# Architecture

## Components

### FlashLock.App

Portable WPF UI. Runs with normal user privileges so simply opening FlashLock does not trigger UAC.

Responsibilities:

- Determine the drive containing the running executable.
- Display volume identity, filesystem, and status.
- Owner setup and PIN entry.
- Request privileged state changes.
- Explain unsupported/recovery states.

### FlashLock.Core

Pure application/domain logic:

- portable drive location
- filesystem compatibility
- PIN hashing/verification
- configuration schema
- protection-state model
- future ACL plan generation and verification

### FlashLock.Elevated (planned)

Small privileged helper launched only for Protect/Unlock/Repair operations.

Responsibilities:

- Re-identify the target volume independently.
- Verify target is not the system volume.
- Verify expected FlashLock metadata/volume identity.
- Back up the current root security descriptor before first protection.
- Apply the protected DACL.
- Restore the saved DACL during unlock.
- Verify postcondition before reporting success.

## State machine

```text
UNINITIALIZED
    |
    | owner setup + PIN
    v
UNLOCKED
    |
    | Protect + UAC
    v
TRANSITIONING_TO_PROTECTED
    |                  |
    | verified         | failed/ambiguous
    v                  v
PROTECTED        RECOVERY_REQUIRED
    |
    | Unlock + PIN + UAC
    v
TRANSITIONING_TO_UNLOCKED
    |                  |
    | restored         | failed/ambiguous
    v                  v
UNLOCKED         RECOVERY_REQUIRED
```

## Target identity

The UI may use the executable root for display, but the privileged helper must not trust a drive letter alone. Drive letters can change. Before mutation it should bind the operation to stable volume metadata and re-check that the FlashLock executable/config live on the same target volume.

## Protection profile

The v1 ACL profile intentionally avoids explicit `Deny` ACEs where possible. A minimal allow-list DACL is easier to reason about:

- SYSTEM: Full Control
- Administrators: Full Control
- Everyone: Read & Execute

The final implementation must be validated on the volume root because root-directory semantics differ from an ordinary child folder.

## Recovery design

Before changing a drive root DACL, FlashLock stores the original SDDL/security descriptor in `.flashlock` and verifies it can be parsed. Unlock restores exactly that descriptor.

A future standalone `FlashLock.Recovery.exe` can repair a drive if the UI/config state is inconsistent.
