using System.Security.AccessControl;
using System.Security.Principal;

namespace FlashLock.Core;

public static class AclProtectionProfile
{
    private static readonly SecurityIdentifier SystemSid = new(WellKnownSidType.LocalSystemSid, null);
    private static readonly SecurityIdentifier AdministratorsSid = new(WellKnownSidType.BuiltinAdministratorsSid, null);
    private static readonly SecurityIdentifier EveryoneSid = new(WellKnownSidType.WorldSid, null);

    public static void Apply(string path, bool isDirectory)
    {
        if (isDirectory)
        {
            var info = new DirectoryInfo(path);
            var security = info.GetAccessControl(AccessControlSections.Access);
            ReplaceRules(security, directory: true);
            info.SetAccessControl(security);
        }
        else
        {
            var info = new FileInfo(path);
            var security = info.GetAccessControl(AccessControlSections.Access);
            ReplaceRules(security, directory: false);
            info.SetAccessControl(security);
        }
    }

    public static void ApplyMetadataTree(string metadataDirectory)
    {
        if (!Directory.Exists(metadataDirectory))
        {
            return;
        }

        var items = Directory.EnumerateFileSystemEntries(metadataDirectory, "*", SearchOption.AllDirectories)
            .Select(path => (Path: path, IsDirectory: Directory.Exists(path)))
            .OrderByDescending(static item => item.Path.Count(static c => c is '\\' or '/'))
            .ToList();

        foreach (var item in items)
        {
            Apply(item.Path, item.IsDirectory);
        }

        Apply(metadataDirectory, isDirectory: true);
    }

    private static void ReplaceRules(FileSystemSecurity security, bool directory)
    {
        security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);

        foreach (FileSystemAccessRule rule in security
                     .GetAccessRules(includeExplicit: true, includeInherited: true, targetType: typeof(SecurityIdentifier))
                     .Cast<FileSystemAccessRule>()
                     .ToArray())
        {
            security.RemoveAccessRuleSpecific(rule);
        }

        var inheritance = directory
            ? InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit
            : InheritanceFlags.None;

        security.AddAccessRule(new FileSystemAccessRule(
            SystemSid,
            FileSystemRights.FullControl,
            inheritance,
            PropagationFlags.None,
            AccessControlType.Allow));

        security.AddAccessRule(new FileSystemAccessRule(
            AdministratorsSid,
            FileSystemRights.FullControl,
            inheritance,
            PropagationFlags.None,
            AccessControlType.Allow));

        security.AddAccessRule(new FileSystemAccessRule(
            EveryoneSid,
            FileSystemRights.ReadAndExecute,
            inheritance,
            PropagationFlags.None,
            AccessControlType.Allow));
    }
}
