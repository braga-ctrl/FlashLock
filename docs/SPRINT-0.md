# Sprint 0 — Feasibility Foundation

## Goal

Prove the Windows mechanism safely before FlashLock ever mutates a real USB root directory.

## Exit criteria

- [x] Product scope and threat model documented.
- [x] .NET 10 WPF/Core solution skeleton added.
- [x] App identifies the drive containing itself.
- [x] App rejects the Windows system drive.
- [x] App detects NTFS compatibility.
- [x] PIN hashing primitive implemented.
- [x] Safe ACL probe provided for a disposable temp folder.
- [x] Architecture defines privilege separation and recovery.
- [ ] Build passes on Windows CI.
- [ ] ACL probe manually validated from a non-elevated Explorer window.
- [ ] ACL behavior repeated on a second Windows machine.
- [ ] Dedicated empty NTFS USB chosen for root-level experiments.

## Manual Windows validation

```powershell
git clone https://github.com/braga-ctrl/FlashLock.git
cd FlashLock
dotnet build FlashLock.sln -c Release
.\tools\Test-AclProtection.ps1
```

Do **not** adapt the probe to your real USB root yet. The next milestone first converts the experiment into a tested protection engine with backup, verification, and rollback.
