# Initial Backlog

## FL-001 — Establish Windows build baseline

- .NET 10 solution builds on Windows.
- GitHub Actions build is green.
- WPF app launches.

## FL-002 — Validate disposable-folder ACL profile

- Read and copy-out work from a non-elevated token.
- Create/modify/rename/delete fail.
- Elevated cleanup/recovery works.
- Results documented on two Windows PCs.

## FL-003 — USB identity and safety guardrails

- Identify the physical/volume identity containing FlashLock.
- Never mutate the system volume.
- Detect filesystem and reject FAT32/exFAT.
- Detect ambiguous/multi-volume cases and fail safely.

## FL-004 — Protected root ACL engine

- Capture original root security descriptor.
- Apply protected DACL to a dedicated empty test USB.
- Read back and verify postcondition.
- Roll back automatically on failure.

## FL-005 — Unlock / exact ACL restore

- Restore the saved original security descriptor.
- Verify write access after restore.
- Preserve backup until verification succeeds.

## FL-006 — First-run owner PIN

- Owner creates PIN/passphrase.
- Salted PBKDF2 verifier stored in `.flashlock`.
- Unlock rejects invalid PIN.
- No plaintext PIN reaches logs or command-line arguments.

## FL-007 — Privileged helper

- UI remains unelevated.
- Protect/unlock invoke a minimal UAC helper.
- Helper independently revalidates target volume.

## FL-008 — Protection-state UX

- Clear Protected / Unlocked / Unsupported / Recovery Required states.
- Show drive label, root, filesystem and size.
- Confirmation before protection transition.

## FL-009 — Cross-machine USB validation matrix

- At least two Windows 11 PCs.
- At least two flash drives/controllers.
- Unplug/replug testing.
- Borrower scenario recorded as demo evidence.

## FL-010 — Portable publish

- Self-contained win-x64 publish.
- Single launchable package suitable for copying onto the USB.
- No machine-wide install required.

## FL-011 — Recovery utility

- Standalone repair path for inconsistent ACL/config state.
- Requires UAC.
- Cannot target the system volume.

## FL-012 — Portfolio demo and release

- README screenshots/GIF.
- Threat-model limitations clearly documented.
- v0.1 release artifact.
