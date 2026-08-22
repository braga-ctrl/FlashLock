# Security Model

## Threat model

FlashLock v1 protects against:

- accidental deletion
- accidental overwrite
- ordinary application writes
- casual modification by a borrower using a normal, non-elevated Windows session

FlashLock v1 does **not** protect against:

- a determined local Administrator
- offline filesystem manipulation by another operating system
- physical attacks or USB-controller reprogramming
- destructive hardware failure
- confidentiality attacks (protected files remain readable by design)

## PIN

The owner PIN/passphrase is never stored directly. The configuration stores a random salt and a PBKDF2-HMAC-SHA256 verifier. Comparisons use constant-time equality.

The PIN is an application control, not a cryptographic lock on the filesystem. A local Administrator can bypass filesystem ACLs without knowing the PIN; documentation and UI must never imply otherwise.

## Privilege separation

The WPF UI runs unelevated. Filesystem security mutations are delegated to a minimal privileged helper via UAC. This reduces the amount of code running with Administrator privileges.

The privileged helper must independently validate:

- target is not the system drive
- filesystem is NTFS
- operation targets the volume that contains FlashLock metadata
- the requested transition matches the current state
- ACL backup exists before destructive ACL replacement

## Fail-safe behavior

- Never auto-format.
- Never auto-select another volume when the executable root is unsupported.
- Never report Protected unless the resulting ACL is read back and verified.
- Never delete the ACL backup while the drive is protected.
- If restoration cannot be verified, enter Recovery Required rather than guessing.

## Admin bypass

Administrators intentionally retain Full Control in v1 for recoverability. A borrower who can elevate is outside the v1 protection guarantee.
