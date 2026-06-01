// ABOUTME: Validates pull request metadata for ISLAMU Contributor License Agreement signatures.
// ABOUTME: Runs from pull_request_target without executing pull-request head code.
#:property RestorePackagesWithLockFile=false

using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;

var eventPath = args.Length > 0 ? args[0] : Environment.GetEnvironmentVariable("GITHUB_EVENT_PATH");
if (string.IsNullOrWhiteSpace(eventPath) || !File.Exists(eventPath))
{
    Console.WriteLine("Missing GitHub event payload path.");
    return 1;
}

using var eventDocument = JsonDocument.Parse(File.ReadAllText(eventPath));
var pullRequest = eventDocument.RootElement.GetProperty("pull_request");
var body = pullRequest.TryGetProperty("body", out var bodyElement) ? bodyElement.GetString() ?? string.Empty : string.Empty;
var author = pullRequest.GetProperty("user").GetProperty("login").GetString() ?? string.Empty;
var commitsUrl = pullRequest.GetProperty("commits_url").GetString() ?? string.Empty;

var trustedBots = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    "dependabot[bot]",
    "github-actions[bot]",
};

if (trustedBots.Contains(author))
{
    Console.WriteLine($"CLA check skipped for trusted bot author {author}.");
    return 0;
}

var requiredSigners = await GetRequiredSignersAsync(commitsUrl, author);
var failures = new List<string>();

if (!HasCheckedClaAgreement(body))
{
    failures.Add("PR body must include the checked ISLAMU CLA agreement checkbox from the pull request template.");
}

var signatures = ExtractSignatureLogins(body);
foreach (var signer in requiredSigners.Order(StringComparer.OrdinalIgnoreCase))
{
    if (!signatures.Contains(signer))
    {
        failures.Add($"Missing CLA Signature line for @{signer}.");
    }
}

if (failures.Count > 0)
{
    Console.WriteLine("ISLAMU CLA validation failed:");
    foreach (var failure in failures)
    {
        Console.WriteLine($"- {failure}");
    }

    Console.WriteLine();
    Console.WriteLine("Required checkbox:");
    Console.WriteLine("- [x] I have read and agree to the ISLAMU Contributor License Agreement in docs/legal/CLA.md.");
    Console.WriteLine();
    Console.WriteLine("Required signature line for each GitHub-linked contributor:");
    Console.WriteLine("CLA Signature: @github-username");
    return 1;
}

Console.WriteLine($"ISLAMU CLA validation passed for {requiredSigners.Count} contributor(s).");
return 0;

static bool HasCheckedClaAgreement(string body)
{
    return Regex.IsMatch(
        body,
        @"(?im)^\s*-\s*\[[xX]\]\s*I\s+have\s+read\s+and\s+agree\s+to\s+the\s+ISLAMU\s+Contributor\s+License\s+Agreement\s+in\s+docs/legal/CLA\.md\.\s*$");
}

static HashSet<string> ExtractSignatureLogins(string body)
{
    var signatures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    var pattern = new Regex(@"(?im)^\s*CLA\s+Signature:\s*@?(?<login>[A-Za-z0-9](?:[A-Za-z0-9-]{0,38}[A-Za-z0-9])?)\s*$");
    foreach (Match match in pattern.Matches(body))
    {
        signatures.Add(match.Groups["login"].Value);
    }

    return signatures;
}

static async Task<HashSet<string>> GetRequiredSignersAsync(string commitsUrl, string author)
{
    var signers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    if (!string.IsNullOrWhiteSpace(author))
    {
        signers.Add(author);
    }

    var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
    if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(commitsUrl))
    {
        return signers;
    }

    using var client = new HttpClient();
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("islamu-cla-validator");
    client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");

    var page = 1;
    while (true)
    {
        var separator = commitsUrl.Contains('?', StringComparison.Ordinal) ? '&' : '?';
        using var response = await client.GetAsync($"{commitsUrl}{separator}per_page=100&page={page}");
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Warning: could not read PR commits from GitHub API ({(int)response.StatusCode}). Validating PR author only.");
            return signers;
        }

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var commits = document.RootElement;
        if (commits.GetArrayLength() == 0)
        {
            break;
        }

        foreach (var commit in commits.EnumerateArray())
        {
            if (commit.TryGetProperty("author", out var commitAuthor)
                && commitAuthor.ValueKind == JsonValueKind.Object
                && commitAuthor.TryGetProperty("login", out var loginElement))
            {
                var login = loginElement.GetString();
                if (!string.IsNullOrWhiteSpace(login))
                {
                    signers.Add(login);
                }
            }
        }

        if (commits.GetArrayLength() < 100)
        {
            break;
        }

        page++;
    }

    return signers;
}
