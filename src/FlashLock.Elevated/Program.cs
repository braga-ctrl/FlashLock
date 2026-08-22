using System.IO.Pipes;
using System.Text.Json;
using FlashLock.Core;

namespace FlashLock.Elevated;

internal static class Program
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task<int> Main(string[] args)
    {
        if (!OperatingSystem.IsWindows())
        {
            return 2;
        }

        var pipeName = GetArgument(args, "--pipe");
        if (string.IsNullOrWhiteSpace(pipeName))
        {
            return 3;
        }

        using var pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(10));
        StreamWriter? writer = null;

        try
        {
            await pipe.ConnectAsync(timeout.Token);
            using var reader = new StreamReader(pipe, leaveOpen: true);
            writer = new StreamWriter(pipe, leaveOpen: true) { AutoFlush = true };
            var line = await reader.ReadLineAsync(timeout.Token)
                ?? throw new InvalidDataException("No FlashLock request received.");

            var request = JsonSerializer.Deserialize<HelperRequest>(line, JsonOptions)
                ?? throw new InvalidDataException("Invalid FlashLock helper request.");

            var engine = new ProtectionEngine();
            ProtectionOperationResult result = request.Action switch
            {
                HelperAction.Protect => await engine.ProtectAsync(
                    request.DriveRoot, Environment.ProcessPath!, request.Pin, request.ExpectedVolumeSerialNumber, timeout.Token),
                HelperAction.Unlock => await engine.UnlockAsync(
                    request.DriveRoot, Environment.ProcessPath!, request.Pin, request.ExpectedVolumeSerialNumber, timeout.Token),
                HelperAction.Recover => await engine.RecoverAsync(
                    request.DriveRoot, Environment.ProcessPath!, request.Pin, request.ExpectedVolumeSerialNumber, timeout.Token),
                _ => throw new ArgumentOutOfRangeException(nameof(request.Action))
            };

            await writer.WriteLineAsync(JsonSerializer.Serialize(
                new HelperResponse(result.Success, result.Message, result.State, result.ObjectsProcessed), JsonOptions));
            return result.Success ? 0 : 1;
        }
        catch (Exception ex)
        {
            if (writer is not null)
            {
                try
                {
                    await writer.WriteLineAsync(JsonSerializer.Serialize(
                        new HelperResponse(false, ex.Message, null), JsonOptions));
                }
                catch { }
            }
            return 1;
        }
        finally
        {
            writer?.Dispose();
        }
    }

    private static string? GetArgument(IReadOnlyList<string> args, string name)
    {
        for (var i = 0; i < args.Count - 1; i++)
        {
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }
        return null;
    }
}
