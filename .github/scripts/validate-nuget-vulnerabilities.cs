// ABOUTME: Parses dotnet vulnerable-package JSON output and fails when any advisory is present.
// ABOUTME: Keeps NuGet audit reporting deterministic without embedding parser logic in workflow YAML.

using System.Text.Json;

var reportPath = args.Length > 0 ? args[0] : "nuget-vulnerabilities.json";
if (!File.Exists(reportPath))
{
    Console.WriteLine($"{reportPath}: NuGet vulnerability report does not exist");
    return 1;
}

using var report = JsonDocument.Parse(File.ReadAllText(reportPath));
var findings = new List<Finding>();
Visit(report.RootElement, project: string.Empty, framework: string.Empty, findings);

if (findings.Count > 0)
{
    Console.WriteLine("NuGet vulnerabilities detected:");
    foreach (var finding in findings)
    {
        var location = string.Join(" / ", new[] { finding.Project, finding.Framework }.Where(static part => !string.IsNullOrWhiteSpace(part)));
        Console.WriteLine($"- {finding.PackageId} ({(string.IsNullOrWhiteSpace(location) ? "unknown project" : location)})");
        foreach (var vulnerability in finding.Vulnerabilities)
        {
            Console.WriteLine($"  - {vulnerability.Severity}: {vulnerability.AdvisoryUrl}");
        }
    }

    return 1;
}

Console.WriteLine("No NuGet vulnerabilities detected.");
return 0;

static void Visit(JsonElement node, string project, string framework, List<Finding> findings)
{
    switch (node.ValueKind)
    {
        case JsonValueKind.Object:
            var currentProject = GetStringProperty(node, "path") ?? project;
            var currentFramework = GetStringProperty(node, "framework") ?? framework;
            var packageId = GetStringProperty(node, "id") ?? "<unknown>";

            if (node.TryGetProperty("vulnerabilities", out var vulnerabilitiesElement)
                && vulnerabilitiesElement.ValueKind == JsonValueKind.Array
                && vulnerabilitiesElement.GetArrayLength() > 0)
            {
                findings.Add(new Finding(
                    currentProject,
                    currentFramework,
                    packageId,
                    vulnerabilitiesElement.EnumerateArray().Select(ReadVulnerability).ToList()));
            }

            foreach (var property in node.EnumerateObject())
            {
                Visit(property.Value, currentProject, currentFramework, findings);
            }

            break;

        case JsonValueKind.Array:
            foreach (var item in node.EnumerateArray())
            {
                Visit(item, project, framework, findings);
            }

            break;
    }
}

static Vulnerability ReadVulnerability(JsonElement vulnerability)
{
    var severity = GetStringProperty(vulnerability, "severity") ?? "unknown";
    var advisoryUrl = GetStringProperty(vulnerability, "advisoryUrl")
        ?? GetStringProperty(vulnerability, "advisoryurl")
        ?? "no advisory URL";

    return new Vulnerability(severity, advisoryUrl);
}

static string? GetStringProperty(JsonElement element, string propertyName)
{
    return element.ValueKind == JsonValueKind.Object
        && element.TryGetProperty(propertyName, out var property)
        && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
}

internal sealed record Finding(string Project, string Framework, string PackageId, IReadOnlyList<Vulnerability> Vulnerabilities);

internal sealed record Vulnerability(string Severity, string AdvisoryUrl);
