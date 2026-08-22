using System.Diagnostics;
using System.IO.Pipes;
using System.Text.Json;
using FlashLock.Core;

namespace FlashLock.App;

public sealed class ElevatedHelperClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<HelperResponse> ExecuteAsync(HelperRequest request, CancellationToken cancellationToken = default)
    {
        var pipeName = "FlashLock-" + Guid.NewGuid().ToString("N");
        await using var pipe = new NamedPipeServerStream(
            pipeName,
            PipeDirection.InOut,
            1,
            PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous);

        var helperPath = FindHelperPath();
        var start = new ProcessStartInfo
        {
            FileName = helperPath,
            Arguments = $"--pipe {pipeName}",
            UseShellExecute = true,
            Verb = "runas",
            WorkingDirectory = Path.GetDirectoryName(helperPath)!
        };

        Process? process;
        try
        {
            process = Process.Start(start);
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            return new(false, "Administrator approval was cancelled.", null);
        }

        if (process is null)
        {
            return new(false, "Unable to start the FlashLock elevated helper.", null);
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMinutes(10));

        try
        {
            await pipe.WaitForConnectionAsync(timeout.Token);
            using var reader = new StreamReader(pipe, leaveOpen: true);
            using var writer = new StreamWriter(pipe, leaveOpen: true) { AutoFlush = true };

            await writer.WriteLineAsync(JsonSerializer.Serialize(request, JsonOptions));
            var responseLine = await reader.ReadLineAsync(timeout.Token);
            if (string.IsNullOrWhiteSpace(responseLine))
            {
                return new(false, "The elevated helper exited without a response.", null);
            }

            return JsonSerializer.Deserialize<HelperResponse>(responseLine, JsonOptions)
                ?? new(false, "The elevated helper returned an invalid response.", null);
        }
        catch (OperationCanceledException)
        {
            return new(false, "The protection operation timed out or was cancelled.", null);
        }
        finally
        {
            try { if (!process.HasExited) process.Kill(entireProcessTree: true); } catch { }
            process.Dispose();
        }
    }

    private static string FindHelperPath()
    {
        var baseDirectory = AppContext.BaseDirectory;
        var published = Path.Combine(baseDirectory, "FlashLock.Elevated.exe");
        if (File.Exists(published))
        {
            return published;
        }

        var configuration = IsDebugBuild ? "Debug" : "Release";
        var candidate = Path.GetFullPath(Path.Combine(
            baseDirectory,
            "..", "..", "..", "..",
            "FlashLock.Elevated", "bin", configuration,
            "net10.0-windows10.0.19041.0", "FlashLock.Elevated.exe"));

        if (File.Exists(candidate))
        {
            return candidate;
        }

        throw new FileNotFoundException(
            "FlashLock.Elevated.exe was not found. Build the full solution or use the published portable package.",
            published);
    }

#if DEBUG
    private const bool IsDebugBuild = true;
#else
    private const bool IsDebugBuild = false;
#endif
}
