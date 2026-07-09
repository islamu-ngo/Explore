// ABOUTME: Architecture tests enforcing accessibility conventions for the dedicated Control Plane host.
// ABOUTME: Verifies shell landmarks, live regions, redirect semantics, and logical CSS in Event.ControlPlane.Blazor.

namespace Event.Architecture.Tests;

using System.Text.RegularExpressions;

public class ControlPlaneAccessibilityConventionTests
{
    private static readonly string ControlPlaneHostRoot = ResolveControlPlaneHostRoot();

    [Test]
    public async Task ControlPlaneLayout_MustContainAccessibleShellLandmarks()
    {
        var content = ReadHostFile("Components/Layout/ControlPlaneLayout.razor");

        await Assert.That(content).Contains("cp-skip-link")
            .Because("Control Plane layout must expose a skip-to-content link.");
        await Assert.That(content).Contains("#main-content")
            .Because("The skip link must target the main landmark.");
        await Assert.That(content).Contains("<main")
            .Because("Control Plane layout must expose a semantic main landmark.");
        await Assert.That(content).Contains("id=\"main-content\"")
            .Because("The main landmark must be the skip-link target.");
        await Assert.That(content).Contains("tabindex=\"-1\"")
            .Because("The main landmark must support programmatic focus after navigation.");
        await Assert.That(content).Contains("<header")
            .Because("Control Plane layout must expose a semantic header landmark.");
        await Assert.That(content).Contains("aria-label=\"Instance console navigation\"")
            .Because("The instance navigation must be explicitly named for assistive technology.");
        await Assert.That(content).Contains("aria-live=\"polite\"")
            .Because("The shell must include a polite live region for non-urgent updates.");
        await Assert.That(content).Contains("aria-live=\"assertive\"")
            .Because("The shell must include an assertive live region for critical updates.");
        await Assert.That(content).Contains("aria-atomic=\"true\"")
            .Because("Live regions must announce their full updated content.");
    }

    [Test]
    public async Task RedirectToLogin_MustNotNestMainLandmarks()
    {
        var content = ReadHostFile("Components/RedirectToLogin.razor");

        await Assert.That(content).DoesNotContain("<main class=\"cp-page\"")
            .Because("RedirectToLogin renders inside ControlPlaneLayout's main landmark.");
        await Assert.That(content).Contains("role=\"status\"")
            .Because("Anonymous login redirects should be announced as non-urgent status updates.");
    }

    [Test]
    public async Task ControlPlaneHostCss_MustUseLogicalDirectionProperties()
    {
        var content = ReadHostFile("wwwroot/css/control-plane.css");
        var physicalDirectionPattern = new Regex(
            @"(?:^|\s|;)\s*(?:margin|padding|border)-(?:left|right|bottom)\s*:",
            RegexOptions.Multiline | RegexOptions.IgnoreCase);

        await Assert.That(physicalDirectionPattern.IsMatch(content)).IsFalse()
            .Because("Control Plane host CSS should use logical properties so the shell can support RTL layouts.");
    }

    private static string ReadHostFile(string relativePath)
    {
        var path = Path.Combine(ControlPlaneHostRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Control Plane host file not found at '{path}'.");
        }

        return File.ReadAllText(path);
    }

    private static string ResolveControlPlaneHostRoot()
    {
        var testDir = AppContext.BaseDirectory;
        var repoRoot = Path.GetFullPath(Path.Combine(testDir, "..", "..", "..", ".."));
        var hostRoot = Path.Combine(repoRoot, "Event.ControlPlane.Blazor");

        if (!Directory.Exists(hostRoot))
        {
            throw new DirectoryNotFoundException(
                $"Event.ControlPlane.Blazor not found at '{hostRoot}'. Test binary dir: '{testDir}'");
        }

        return hostRoot;
    }
}
