// ABOUTME: Creates one new secret artifact beneath a fixed directory with explicit Unix owner-only permissions.
// ABOUTME: Refuses unsupported platforms, unsafe filenames, overwrite, backup, and plaintext fallback behavior.

namespace ISLAMU.Event.SetupAssistant.Cli.Tui;

public sealed class UnixSetupTerminalProtectedWriter : ISetupTerminalProtectedWriter
{
    private const UnixFileMode OwnerOnly = UnixFileMode.UserRead | UnixFileMode.UserWrite;
    private readonly string _baseDirectory;

    public UnixSetupTerminalProtectedWriter(string baseDirectory)
    {
        ArgumentException.ThrowIfNullOrEmpty(baseDirectory);
        _baseDirectory = Path.GetFullPath(baseDirectory);
    }

    public bool IsAvailable => OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() || OperatingSystem.IsFreeBSD();

    public SetupTerminalProtectedWriteResult WriteCreateNew(
        string validatedFileName, ReadOnlyMemory<byte> bytes, int maximumBytes)
    {
        if (OperatingSystem.IsWindows() || !IsAvailable
            || !SetupPublicFileNameBuffer.IsSafe(validatedFileName)
            || bytes.Length == 0 || bytes.Length > maximumBytes)
            return SetupTerminalProtectedWriteResult.Blocked;

        string path = Path.Combine(_baseDirectory, validatedFileName);
        bool created = false;
        try
        {
            var options = new FileStreamOptions
            {
                Mode = FileMode.CreateNew,
                Access = FileAccess.Write,
                Share = FileShare.None,
                Options = FileOptions.WriteThrough,
                UnixCreateMode = OwnerOnly,
            };
            using var stream = new FileStream(path, options);
            created = true;
            File.SetUnixFileMode(stream.SafeFileHandle, OwnerOnly);
            stream.Write(bytes.Span);
            stream.Flush(flushToDisk: true);
            if (File.GetUnixFileMode(stream.SafeFileHandle) != OwnerOnly)
                throw new IOException("protected-output-mode-invalid");
            return SetupTerminalProtectedWriteResult.Written;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            if (created)
            {
                try { File.Delete(path); }
                catch (Exception cleanup) when (cleanup is IOException or UnauthorizedAccessException) { }
            }
            return SetupTerminalProtectedWriteResult.Blocked;
        }
    }

    public override string ToString() => $"{nameof(UnixSetupTerminalProtectedWriter)}:Available={IsAvailable}";
}
