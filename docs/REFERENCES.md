# Technical References

FlashLock's feasibility decisions are grounded in Windows' documented storage and access-control behavior.

## Microsoft Learn

- **Disk read-only attribute:** `attributes disk` documents `attributes disk set readonly` for Windows 10/11.  
  https://learn.microsoft.com/en-us/windows-server/administration/windows-commands/attributes-disk

- **Low-level disk attributes:** `SET_DISK_ATTRIBUTES` exposes `DISK_ATTRIBUTE_READ_ONLY`; its `Persist` member documents persistence across reboots.  
  https://learn.microsoft.com/en-us/windows/win32/api/winioctl/ns-winioctl-set_disk_attributes

- **File security and access rights:** Windows files are securable objects whose access is controlled by security descriptors and ACLs.  
  https://learn.microsoft.com/en-us/windows/win32/fileio/file-security-and-access-rights

- **SetNamedSecurityInfo:** documents setting DACL/security information for files and directories on NTFS, including propagation of inheritable ACEs.  
  https://learn.microsoft.com/en-us/windows/win32/api/aclapi/nf-aclapi-setnamedsecurityinfoa

- **Access-control lists:** overview of DACLs and how Windows grants/denies access.  
  https://learn.microsoft.com/en-us/windows/win32/secauthz/access-control-lists

- **icacls:** documents Windows ACL backup/restore, inheritance, and read/write/delete permission masks used by the safe probe.  
  https://learn.microsoft.com/en-us/windows-server/administration/windows-commands/icacls

- **WPF / .NET 10:** .NET 10 was released in November 2025 and WPF is actively supported in that release.  
  https://learn.microsoft.com/en-us/dotnet/desktop/wpf/whats-new/net100

## Engineering conclusion

The documented disk attribute is useful but does not itself establish that a read-only state travels to another computer. FlashLock therefore treats NTFS DACLs as the portable Windows mechanism and reserves disk-level read-only as optional defense-in-depth after cross-machine testing.
