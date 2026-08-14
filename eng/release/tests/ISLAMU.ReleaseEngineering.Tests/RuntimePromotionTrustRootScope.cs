// ABOUTME: Serializes tests that mutate the release-engine runtime promotion trust-root singleton.
// ABOUTME: Snapshots exact prior bytes or absence, atomically writes fixture bytes, and restores on dispose.

namespace ISLAMU.ReleaseEngineering.Tests;

internal sealed class RuntimePromotionTrustRootScope : IDisposable
{
    private const string RuntimeTrustRootName = "ISLAMU.ReleaseEngineering.promotion-allowed-signers";
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private readonly FileStream processLock;
    private readonly byte[]? originalBytes;
    private bool disposed;

    private RuntimePromotionTrustRootScope(string sourcePath)
    {
        Gate.Wait();
        try
        {
            RuntimeTrustRootPath = GetRuntimeTrustRootPath();
            processLock = AcquireProcessLock(RuntimeTrustRootPath + ".lock");
            originalBytes = File.Exists(RuntimeTrustRootPath) ? File.ReadAllBytes(RuntimeTrustRootPath) : null;
            AtomicWrite(RuntimeTrustRootPath, File.ReadAllBytes(sourcePath));
        }
        catch
        {
            Gate.Release();
            throw;
        }
    }

    public string RuntimeTrustRootPath { get; }

    public static RuntimePromotionTrustRootScope Use(string sourcePath) => new(sourcePath);

    public static RuntimePromotionTrustRootScope UsePackagedDefault() => new(Path.Combine(RepositoryRoot.Find(), "eng", "release", "trust", "promotion-allowed-signers"));

    public static string GetRuntimeTrustRootPath() => Path.Combine(AppContext.BaseDirectory, RuntimeTrustRootName);

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        try
        {
            if (originalBytes is null)
            {
                if (File.Exists(RuntimeTrustRootPath)) File.Delete(RuntimeTrustRootPath);
            }
            else
            {
                AtomicWrite(RuntimeTrustRootPath, originalBytes);
            }
        }
        finally
        {
            processLock.Dispose();
            Gate.Release();
        }
    }

    private static FileStream AcquireProcessLock(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
        while (true)
        {
            try
            {
                return new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException) when (stopwatch.Elapsed < TimeSpan.FromSeconds(30))
            {
                Thread.Sleep(25);
            }
        }
    }

    private static void AtomicWrite(string destinationPath, byte[] bytes)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        string tempPath = Path.Combine(Path.GetDirectoryName(destinationPath)!, $".{Path.GetFileName(destinationPath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            File.WriteAllBytes(tempPath, bytes);
            File.Move(tempPath, destinationPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }
}
