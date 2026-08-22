# FlashLock

> Portable USB write protection for Windows.

FlashLock is a Windows desktop utility designed to live **on the USB flash drive it protects**. In protected mode, normal users can open and copy files from the drive, but cannot delete, rename, overwrite, or add content. The owner can run `FlashLock.exe`, authenticate, approve UAC elevation, and return the drive to normal read/write mode.

## Product promise

**Protected**

- Read and open files
- Copy files from USB to the computer
- Block normal delete / rename / overwrite
- Block creating new files and folders
- Block accidental application writes

**Unlocked**

- Normal read/write behavior
- Owner can update, delete, rename, or add files

## Important security boundary

FlashLock v1 is **software write protection, not hardware write protection**. It is intended to stop accidental deletion and ordinary unauthorized modification on Windows PCs. A user with local Administrator rights can ultimately take ownership or replace ACLs, so the project does not claim to be tamper-proof.

The portable protection profile requires **NTFS** because Windows ACLs are stored with NTFS files and directories. FAT32/exFAT drives cannot provide the same filesystem-level access control.

## Why ACLs instead of only `diskpart readonly`?

Windows can set a disk read-only, but that state is a Windows disk attribute and is not a reliable portable lock when the USB is moved to another computer. FlashLock therefore treats **NTFS ACLs as the primary portable mechanism** and may later use the disk read-only attribute as optional same-machine hardening.

## Target architecture

```text
USB flash drive (NTFS)
|
|-- FlashLock.exe              portable UI, runs unelevated
|-- FlashLock.Elevated.exe     future privileged helper
|-- .flashlock/
|   |-- config.json            PIN verifier + protection metadata
|   `-- acl-backup.json        original ACL state for recovery
|
`-- user files...

FlashLock.exe
    |
    | identify the volume containing itself
    | verify NTFS + protection state
    v
Owner action + PIN
    |
    v
UAC elevation
    |
    v
Privileged helper
    |
    | set / restore root NTFS DACL
    v
USB protected / unlocked
```

## Stack

- C# / **.NET 10**
- WPF desktop UI
- Win32 / NTFS access-control APIs
- Self-contained Windows publish for portable use
- No cloud account, server, or Internet requirement

## Repository status

**Phase:** Sprint 0 / feasibility foundation

The repository includes a compile-ready WPF/Core skeleton and a **safe ACL probe** that only modifies a disposable temporary folder. The real drive-root mutation engine is deliberately deferred until its behavior is validated on multiple Windows PCs and USB devices.

## Build

Requirements: Windows 10/11 and .NET 10 SDK.

```powershell
dotnet restore
dotnet build FlashLock.sln -c Release
```

Run the desktop app:

```powershell
dotnet run --project .\src\FlashLock.App\FlashLock.App.csproj
```

Run the non-destructive ACL experiment:

```powershell
.\tools\Test-AclProtection.ps1
```

## Read next

- [`docs/FEASIBILITY.md`](docs/FEASIBILITY.md)
- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md)
- [`docs/SECURITY-MODEL.md`](docs/SECURITY-MODEL.md)
- [`docs/SPRINT-0.md`](docs/SPRINT-0.md)
- [`docs/BACKLOG.md`](docs/BACKLOG.md)
- [`docs/adr`](docs/adr)
