// ABOUTME: Guards the admission QR scanner module against credential persistence and disclosure sinks.
// ABOUTME: Requires native secure-context feature detection and caller-owned image-source detection only.

namespace Explore.Blazor.Client.Tests.Security;

public sealed class AdmissionQrScannerSafetyTests
{
    [Test]
    public async Task ScannerModuleUsesOnlyNativeSecureQrDetection()
    {
        string source = await File.ReadAllTextAsync(ModulePath());

        await Assert.That(source).Contains("globalThis.isSecureContext");
        await Assert.That(source).Contains("globalThis.BarcodeDetector");
        await Assert.That(source).Contains("getSupportedFormats");
        await Assert.That(source).Contains("qr_code");
        await Assert.That(source).Contains("detector.detect(imageSource)");
        await Assert.That(source).DoesNotContain("ZXing");
        await Assert.That(source).DoesNotContain("QRCoder");
    }

    [Test]
    public async Task ScannerModuleHasNoCredentialLeakageSinks()
    {
        string source = await File.ReadAllTextAsync(ModulePath());
        string executable = string.Join('\n', source.Split('\n')
            .Where(line => !line.TrimStart().StartsWith("//", StringComparison.Ordinal)));

        foreach (string forbidden in new[]
                 {
                     "localStorage", "sessionStorage", "indexedDB", "document.", "innerHTML",
                     "outerHTML", "setAttribute", "fetch(", "XMLHttpRequest", "sendBeacon",
                     "WebSocket", "console.", "telemetry", "location.", "history.", "eval(",
                     "referrer", "URL("
                 })
        {
            await Assert.That(executable).DoesNotContain(forbidden);
        }
    }

    private static string ModulePath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
        {
            directory = directory.Parent;
        }

        return Path.Combine(directory!.FullName, "src", "Explore.Blazor.Client", "wwwroot", "js", "admission-qr-scanner.js");
    }
}
