using System.Security.AccessControl;
using System.Security.Principal;

namespace FlashLock.Core;

public static class ProtectionVerifier
{
    private static readonly SecurityIdentifier SystemSid = new(WellKnownSidType.LocalSystemSid, null);
    private static readonly SecurityIdentifier AdministratorsSid = new(WellKnownSidType.BuiltinAdministratorsSid, null);
    private static readonly SecurityIdentifier EveryoneSid = new(WellKnownSidType.WorldSid, null);

    private const FileSystemRights ForbiddenNormalUserRights =
        FileSystemRights.WriteData |
        FileSystemRights.AppendData |
        FileSystemRights.WriteExtendedAttributes |
        FileSystemRights.DeleteSubdirectoriesAndFiles |
        FileSystemRights.WriteAttributes |
        FileSystemRights.Delete |
        FileSystemRights.ChangePermissions |
        FileSystemRights.TakeOwnership;

    public static void VerifyProtected(string path, bool isDirectory)
    {
        FileSystemSecurity security = isDirectory
            ? new DirectoryInfo(path).GetAccessControl(AccessControlSections.Access)
            : new FileInfo(path).GetAccessControl(AccessControlSections.Access);

        if (!security.AreAccessRulesProtected)
        {
            throw new InvalidOperationException($"Protection verification failed because ACL inheritance remains enabled: {path}");
        }

        var rules = security
            .GetAccessRules(includeExplicit: true, includeInherited: true, targetType: typeof(SecurityIdentifier))
            .Cast<FileSystemAccessRule>()
            .ToList();

        if (rules.Any(static rule => rule.AccessControlType != AccessControlType.Allow))
        {
            throw new InvalidOperationException($"Protection verification found an unexpected deny rule: {path}");
        }

        VerifyFullControl(rules, SystemSid, path);
        VerifyFullControl(rules, AdministratorsSid, path);
        VerifyReadOnly(rules, EveryoneSid, path);

        var allowedSids = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            SystemSid.Value,
            AdministratorsSid.Value,
            EveryoneSid.Value
        };

        if (rules.Any(rule => rule.IdentityReference is SecurityIdentifier sid && !allowedSids.Contains(sid.Value)))
        {
            throw new InvalidOperationException($"Protection verification found an unexpected access principal: {path}");
        }
    }

    public static bool ContainsForbiddenNormalUserRights(FileSystemRights rights) =>
        (rights & ForbiddenNormalUserRights) != 0;

    private static void VerifyFullControl(IReadOnlyList<FileSystemAccessRule> rules, SecurityIdentifier sid, string path)
    {
        var matching = rules.Where(rule => rule.IdentityReference.Equals(sid)).ToList();
        if (matching.Count != 1 || (matching[0].FileSystemRights & FileSystemRights.FullControl) != FileSystemRights.FullControl)
        {
            throw new InvalidOperationException($"Protection verification did not find the required recovery Full Control rule: {path}");
        }
    }

    private static void VerifyReadOnly(IReadOnlyList<FileSystemAccessRule> rules, SecurityIdentifier sid, string path)
    {
        var matching = rules.Where(rule => rule.IdentityReference.Equals(sid)).ToList();
        if (matching.Count != 1)
        {
            throw new InvalidOperationException($"Protection verification did not find exactly one Everyone rule: {path}");
        }

        var rights = matching[0].FileSystemRights;
        if ((rights & FileSystemRights.ReadAndExecute) != FileSystemRights.ReadAndExecute || ContainsForbiddenNormalUserRights(rights))
        {
            throw new InvalidOperationException($"Protection verification found write-capable Everyone permissions: {path}");
        }
    }
}
