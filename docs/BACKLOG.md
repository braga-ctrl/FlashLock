# v0.1 Backlog / Status

## FL-001 — Establish Windows build baseline

**Implementation:** complete. **Validation:** pending/CI gate.

- .NET 10 solution includes Core, WPF App, Elevated helper and xUnit tests.
- GitHub Actions builds/tests on `windows-latest`.

## FL-002 — Validate disposable-folder ACL profile

**Implementation:** complete. **Manual validation:** required on Windows.

- `tools/Test-AclProtection.ps1` self-elevates, applies the proposed ACL only to a disposable temp folder, and cleans up afterward.

## FL-003 — USB identity and safety guardrails

**Implemented.**

- target must be the drive containing the helper executable
- system drive is refused
- NTFS required; FAT32/exFAT refused
- volume serial bound to each request/config
- reparse points refused in protected user content

## FL-004 — Protected root ACL engine

**Implemented; dedicated-test-USB validation pending.**

- complete user-data DACL snapshot before mutation
- protected ACL applied deepest-first/root-last
- protected ACL read-back verification
- automatic rollback on failure

## FL-005 — Unlock / exact ACL restore

**Implemented; hardware validation pending.**

- saved DACLs restored deepest-first/root-last
- restored SDDL read back and verified
- Recovery Required state retained on failure

## FL-006 — First-run owner PIN

**Implemented.**

- PBKDF2-HMAC-SHA256, random salt, 600,000 iterations
- no plaintext PIN stored or placed on process command line
- five failed attempts trigger a short lockout

## FL-007 — Privileged helper

**Implemented.**

- WPF UI stays unelevated
- UAC helper has `requireAdministrator`
- random named pipe carries the in-memory request
- helper independently validates target drive and serial

## FL-008 — Protection-state UX

**Implemented.**

- Not Configured / Unlocked / Protected / Recovery Needed / Unsupported
- drive label, root, filesystem, size and volume ID
- PIN creation/entry and recovery flow

## FL-009 — Cross-machine USB validation matrix

**Tooling implemented; physical validation pending.**

- `tools/Validate-FlashLock.ps1`
- required: two PCs, two USB devices, unplug/replug cycles

## FL-010 — Portable publish

**Implemented.**

- self-contained `win-x64` publish for App and Elevated helper
- Actions workflow assembles `FlashLock-win-x64.zip`

## FL-011 — Recovery utility

**Implemented.**

- UI Recovery action
- standalone `tools/Manual-Recover-Acl.ps1`
- system-drive refusal

## FL-012 — Portfolio demo and v0.1 release

**Pending hardware validation and screenshots/demo capture.**
