namespace FlashLock.Core;

public sealed record ProtectionOperationResult(bool Success, string Message, ProtectionState State, int ObjectsProcessed = 0);

public sealed class ProtectionEngine
{
    private readonly ConfigStore _configStore = new();
    private readonly AclSnapshotStore _snapshotStore = new();

    public async Task<ProtectionOperationResult> ProtectAsync(
        string driveRoot,
        string executablePath,
        string pin,
        string expectedVolumeSerial,
        CancellationToken cancellationToken = default)
    {
        var drive = DriveSafetyGuard.EnsureSafeTarget(driveRoot, executablePath, expectedVolumeSerial);
        var config = await _configStore.LoadAsync(drive.RootPath, cancellationToken);
        var now = DateTimeOffset.UtcNow;

        if (config is not null)
        {
            config = await AuthenticateAsync(drive.RootPath, config, pin, now, cancellationToken);
            if (config.State == ProtectionState.Protected)
            {
                return new(true, "Drive is already protected.", ProtectionState.Protected);
            }

            if (config.State is ProtectionState.Applying or ProtectionState.Restoring or ProtectionState.RecoveryRequired)
            {
                throw new InvalidOperationException("An interrupted protection operation requires recovery before protecting again.");
            }
        }
        else
        {
            config = new FlashLockConfig(
                FlashLockConfig.CurrentSchemaVersion,
                now,
                PinHasher.Create(pin),
                ProtectionState.Unlocked,
                drive.VolumeSerialNumber,
                FlashLockConfig.DefaultSnapshotFileName,
                null,
                now,
                0,
                null);
            await _configStore.SaveAsync(drive.RootPath, config, cancellationToken);
        }

        if (!string.Equals(config.VolumeSerialNumber, drive.VolumeSerialNumber, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("FlashLock configuration belongs to a different volume identity.");
        }

        var snapshotPath = _configStore.GetSnapshotPath(drive.RootPath, config.SnapshotFileName);
        var applying = config with { State = ProtectionState.Applying };
        await _configStore.SaveAsync(drive.RootPath, applying, cancellationToken);

        var count = 0;
        try
        {
            count = await _snapshotStore.CaptureAsync(drive.RootPath, snapshotPath, cancellationToken);
            var objects = _snapshotStore.EnumerateProtectedObjects(drive.RootPath)
                .OrderByDescending(static x => PathDepth(x.RelativePath))
                .ToList();

            foreach (var item in objects)
            {
                cancellationToken.ThrowIfCancellationRequested();
                AclProtectionProfile.Apply(item.FullPath, item.IsDirectory);
            }

            AclProtectionProfile.ApplyMetadataTree(_configStore.GetMetadataDirectory(drive.RootPath));

            foreach (var item in objects)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ProtectionVerifier.VerifyProtected(item.FullPath, item.IsDirectory);
            }

            ProtectionVerifier.VerifyProtected(_configStore.GetMetadataDirectory(drive.RootPath), isDirectory: true);

            var protectedConfig = applying with
            {
                State = ProtectionState.Protected,
                ProtectedAtUtc = now,
                FailedPinAttempts = 0,
                PinLockoutUntilUtc = null
            };
            await _configStore.SaveAsync(drive.RootPath, protectedConfig, cancellationToken);
            return new(true, $"Protected {count} filesystem objects.", ProtectionState.Protected, count);
        }
        catch (Exception original)
        {
            try
            {
                if (File.Exists(snapshotPath))
                {
                    await _snapshotStore.RestoreAsync(drive.RootPath, snapshotPath, CancellationToken.None);
                }

                var rolledBack = applying with { State = ProtectionState.Unlocked, LastUnlockedAtUtc = DateTimeOffset.UtcNow };
                await _configStore.SaveAsync(drive.RootPath, rolledBack, CancellationToken.None);
            }
            catch
            {
                try
                {
                    await _configStore.SaveAsync(
                        drive.RootPath,
                        applying with { State = ProtectionState.RecoveryRequired },
                        CancellationToken.None);
                }
                catch
                {
                    // At this point the snapshot is still retained for manual recovery.
                }
            }

            throw new InvalidOperationException("Protection failed. FlashLock attempted rollback; run Recovery if the drive does not behave normally.", original);
        }
    }

    public async Task<ProtectionOperationResult> UnlockAsync(
        string driveRoot,
        string executablePath,
        string pin,
        string expectedVolumeSerial,
        CancellationToken cancellationToken = default) =>
        await RestoreAsync(driveRoot, executablePath, pin, expectedVolumeSerial, recovery: false, cancellationToken);

    public async Task<ProtectionOperationResult> RecoverAsync(
        string driveRoot,
        string executablePath,
        string pin,
        string expectedVolumeSerial,
        CancellationToken cancellationToken = default) =>
        await RestoreAsync(driveRoot, executablePath, pin, expectedVolumeSerial, recovery: true, cancellationToken);

    private async Task<ProtectionOperationResult> RestoreAsync(
        string driveRoot,
        string executablePath,
        string pin,
        string expectedVolumeSerial,
        bool recovery,
        CancellationToken cancellationToken)
    {
        var drive = DriveSafetyGuard.EnsureSafeTarget(driveRoot, executablePath, expectedVolumeSerial);
        var config = await _configStore.LoadAsync(drive.RootPath, cancellationToken)
            ?? throw new InvalidOperationException("This drive has not been configured with FlashLock.");

        config = await AuthenticateAsync(drive.RootPath, config, pin, DateTimeOffset.UtcNow, cancellationToken);

        if (!string.Equals(config.VolumeSerialNumber, drive.VolumeSerialNumber, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("FlashLock configuration belongs to a different volume identity.");
        }

        if (!recovery && config.State == ProtectionState.Unlocked)
        {
            return new(true, "Drive is already unlocked.", ProtectionState.Unlocked);
        }

        var snapshotPath = _configStore.GetSnapshotPath(drive.RootPath, config.SnapshotFileName);
        var restoring = config with { State = ProtectionState.Restoring };
        await _configStore.SaveAsync(drive.RootPath, restoring, cancellationToken);

        try
        {
            await _snapshotStore.RestoreAsync(drive.RootPath, snapshotPath, cancellationToken);
            var unlocked = restoring with
            {
                State = ProtectionState.Unlocked,
                LastUnlockedAtUtc = DateTimeOffset.UtcNow,
                FailedPinAttempts = 0,
                PinLockoutUntilUtc = null
            };
            await _configStore.SaveAsync(drive.RootPath, unlocked, cancellationToken);
            TryDeleteSnapshot(snapshotPath);
            return new(true, "Original filesystem permissions restored.", ProtectionState.Unlocked);
        }
        catch (Exception ex)
        {
            try
            {
                await _configStore.SaveAsync(
                    drive.RootPath,
                    restoring with { State = ProtectionState.RecoveryRequired },
                    CancellationToken.None);
            }
            catch
            {
                // Snapshot remains on disk for manual recovery.
            }

            throw new InvalidOperationException("Restore did not complete. The ACL snapshot was retained; run Recovery again.", ex);
        }
    }

    private async Task<FlashLockConfig> AuthenticateAsync(
        string root,
        FlashLockConfig config,
        string pin,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        PinAttemptPolicy.ThrowIfLockedOut(config, now);
        if (!PinHasher.Verify(pin, config.OwnerPin))
        {
            var failed = PinAttemptPolicy.RecordFailure(config, now);
            await _configStore.SaveAsync(root, failed, cancellationToken);
            throw new UnauthorizedAccessException("Incorrect owner PIN.");
        }

        var success = PinAttemptPolicy.RecordSuccess(config);
        if (success != config)
        {
            await _configStore.SaveAsync(root, success, cancellationToken);
        }
        return success;
    }

    private static int PathDepth(string relativePath) =>
        relativePath == "." ? -1 : relativePath.Count(static c => c is '\\' or '/') + 1;

    private static void TryDeleteSnapshot(string snapshotPath)
    {
        try { File.Delete(snapshotPath); } catch { }
    }
}
