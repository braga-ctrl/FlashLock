# FlashLock

> Portable USB write protection for Windows.

FlashLock is a Windows desktop utility designed to live **on the NTFS USB flash drive it protects**. In protected mode, normal users can open and copy files from the drive, but cannot delete, rename, overwrite, or add content. The owner can run `FlashLock.exe`, enter the owner PIN, approve UAC elevation, and restore the exact original filesystem ACLs.

## Current status

**v0.1 implementation candidate**

The protection engine, PIN flow, elevated helper, rollback snapshot, recovery path, Windows CI, portable publish workflow, and validation scripts are implemented. Before using FlashLock with irreplaceable data, complete the manual USB validation matrix in [`docs/VALIDATION.md`](docs/VALIDATION.md), including a second Windows PC.

## Product behavior

**Protected**

- Read and open files
- Copy files from USB to another drive
- Block normal delete / rename / overwrite
- Block creating new files and folders
- Keep `FlashLock.exe` runnable

**Unlocked**

- Restore the original DACL of each protected filesystem object
- Normal read/write behavior returns
- Owner can update, delete, rename, or add files

## Security boundary

FlashLock v0.1 is **software write protection, not hardware write protection or encryption**. It targets accidental deletion and ordinary unauthorized modification from a normal Windows session. A local Administrator can deliberately take ownership or replace ACLs, so FlashLock does not claim to resist an administrator, another operating system with raw NTFS access, or physical attacks.

Files remain readable by design.

## Why NTFS ACLs?

Windows supports disk-level read-only attributes, but that state is not a sufficient portable promise when a USB is moved to another computer. FlashLock uses **NTFS file/directory ACLs stored with the filesystem** as its primary mechanism.

Protected objects receive an allow-list DACL:

- `SYSTEM` — Full Control
- `BUILTIN\Administrators` — Full Control
- `Everyone` — Read & Execute

FAT32 and exFAT are rejected. FlashLock never formats a drive.

## Architecture

```text
USB flash drive (NTFS)
|
|-- FlashLock.exe              WPF UI, normal user token
|-- FlashLock.Elevated.exe     UAC helper, Administrator token
|-- .flashlock/
|   |-- config.json            PIN verifier + volume/state metadata
|   `-- acl-snapshot.jsonl     exact original DACL snapshot while protected
|
`-- user files...

FlashLock.exe
    |
    | detect its own volume + serial
    | owner PIN
    v
named pipe + UAC
    |
    v
FlashLock.Elevated.exe
    |
    | independently revalidate target
    | snapshot every user-file DACL before mutation
    | apply and verify read-only ACLs
    | rollback automatically on failure
    v
PROTECTED

Unlock / Recovery
    |
    | verify owner PIN
    | restore saved DACLs deepest-first
    | read back each restored DACL
    v
UNLOCKED
```

## Safety invariants

FlashLock will:

- only target the drive containing the running FlashLock executable
- refuse the Windows system drive
- require NTFS
- bind operations to the detected volume serial number
- refuse reparse points in protected user content for v0.1
- create and flush a complete ACL snapshot before changing user-file permissions
- verify the protected ACL profile before reporting `PROTECTED`
- retain recovery state when restore cannot be verified
- never auto-format

## Stack

- C# / **.NET 10**
- WPF desktop UI
- Windows NTFS access-control APIs
- UAC privilege separation through a minimal helper
- PBKDF2-HMAC-SHA256 owner PIN verifier
- xUnit tests
- GitHub Actions on `windows-latest`
- self-contained `win-x64` portable publish

## Build

Requirements: Windows 10/11 and .NET 10 SDK.

```powershell
dotnet restore FlashLock.sln
dotnet build FlashLock.sln -c Release
dotnet test FlashLock.sln -c Release
```

Run during development:

```powershell
dotnet run --project .\src\FlashLock.App\FlashLock.App.csproj
```

The app deliberately refuses real protection when launched from the Windows system drive. Use the published package from an NTFS test USB for end-to-end testing.

## Safe validation sequence

First validate the ACL concept without touching a USB root:

```powershell
.\tools\Test-AclProtection.ps1
```

Then follow the dedicated test-stick workflow:

```powershell
.\tools\Validate-FlashLock.ps1 -Mode Prepare -DriveRoot E:\
# Protect with FlashLock.exe
.\tools\Validate-FlashLock.ps1 -Mode VerifyProtected -DriveRoot E:\
# Repeat VerifyProtected on a second Windows PC
# Unlock with FlashLock.exe
.\tools\Validate-FlashLock.ps1 -Mode VerifyUnlocked -DriveRoot E:\
```

See [`docs/VALIDATION.md`](docs/VALIDATION.md) before trusting v0.1 with important data.

## Emergency recovery

If the GUI cannot complete an ACL restore but `.flashlock\acl-snapshot.jsonl` exists, an Administrator can run:

```powershell
.\tools\Manual-Recover-Acl.ps1 -DriveRoot E:\
```

The recovery script refuses the Windows system drive. See [`docs/RECOVERY.md`](docs/RECOVERY.md).

## Read next

- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md)
- [`docs/SECURITY-MODEL.md`](docs/SECURITY-MODEL.md)
- [`docs/VALIDATION.md`](docs/VALIDATION.md)
- [`docs/RECOVERY.md`](docs/RECOVERY.md)
- [`docs/BACKLOG.md`](docs/BACKLOG.md)
- [`docs/adr`](docs/adr)
