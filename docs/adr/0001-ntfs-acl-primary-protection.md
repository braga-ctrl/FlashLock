# ADR 0001 — Use NTFS ACLs as the primary portable protection mechanism

**Status:** Accepted for feasibility validation

## Context

FlashLock's protection must travel with the USB to another Windows computer. Windows disk read-only state alone is not a reliable cross-computer guarantee.

## Decision

Require NTFS for v1 and store the protection state in the filesystem's security descriptors. Use a protected root DACL that grants normal users read/execute and retains full control for SYSTEM/Administrators.

## Consequences

- Portable across normal Windows NTFS mounts.
- FAT32/exFAT unsupported for v1.
- Local Administrators can bypass protection.
- Exact ACL backup/restore becomes safety-critical.
