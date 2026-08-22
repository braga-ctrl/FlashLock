# ADR 0003 — Separate the normal UI from privileged mutations

**Status:** Accepted

## Context

Changing root NTFS ACLs requires privileges, but running the entire UI elevated increases risk and creates unnecessary UAC prompts.

## Decision

Run `FlashLock.exe` asInvoker. Add a small `FlashLock.Elevated` helper for Protect, Unlock, and Recovery operations. The helper revalidates the target volume rather than trusting UI-supplied drive letters.

## Consequences

- Better least-privilege posture.
- Additional IPC/operation-authentication design is required.
- Privileged code remains small and auditable.
