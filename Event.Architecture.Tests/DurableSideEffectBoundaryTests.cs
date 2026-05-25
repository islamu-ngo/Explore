// ABOUTME: Architecture guardrails for durable side-effect execution boundaries.
// ABOUTME: Prevents handlers and controllers from bypassing EmailDispatchOutbox or broker transport abstractions.

namespace Event.Architecture.Tests;

using System.Text.RegularExpressions;

public sealed class DurableSideEffectBoundaryTests
{
    private static readonly Regex DirectSmtpSendPattern = new(@"\.SendAsync\s*\(|\.SendMailAsync\s*\(", RegexOptions.Compiled);
    private static readonly Regex BrokerMethodPattern = new(@"\.PublishAsync\s*\(|\.BulkPublishAsync\s*\(|\.SubscribeAsync\s*\(", RegexOptions.Compiled);
    private static readonly Regex DirectBrokerOperationPattern = new(
        @"BasicPublish\s*\(|BasicAck\s*\(|BasicReject\s*\(|BasicNack\s*\(|RabbitMQ\.Client|MQContract\.RabbitMQ",
        RegexOptions.Compiled);

    [Test]
    public async Task ApplicationHandlersShouldNotSendEmailOrPublishBrokerMessagesDirectly()
    {
        string repoRoot = FindRepoRoot();
        string featuresRoot = Path.Combine(repoRoot, "Explore.Application", "Features");

        var violations = new List<string>();

        foreach (string file in EnumerateHandlerSourceFiles(featuresRoot))
        {
            string content = await File.ReadAllTextAsync(file);
            if (ReferencesEmailTransport(content))
            {
                AddForbiddenMatches(violations, file, content, DirectSmtpSendPattern, "direct SMTP send");
            }

            AddForbiddenMatches(violations, file, content, DirectBrokerOperationPattern, "direct broker operation");

            if (ReferencesBrokerTransport(content))
            {
                AddForbiddenMatches(violations, file, content, BrokerMethodPattern, "direct broker operation");
            }

            if (ReferencesEmailTransport(content) || ReferencesBrokerTransport(content))
            {
                violations.Add($"{GetRelativePath(repoRoot, file)} references a side-effect transport contract; create durable intent instead.");
            }
        }

        await Assert.That(violations).IsEmpty()
            .Because("Application handlers must create durable intent rows and leave SMTP/RabbitMQ side effects to approved background infrastructure.");
    }

    [Test]
    public async Task ApiControllersShouldNotSendEmailOrPublishBrokerMessagesDirectly()
    {
        string repoRoot = FindRepoRoot();
        string controllersRoot = Path.Combine(repoRoot, "Explore.API", "Controllers");

        var violations = new List<string>();

        foreach (string file in EnumerateSourceFiles(controllersRoot, "*.cs"))
        {
            string content = await File.ReadAllTextAsync(file);
            if (ReferencesEmailTransport(content))
            {
                AddForbiddenMatches(violations, file, content, DirectSmtpSendPattern, "direct SMTP send");
            }

            AddForbiddenMatches(violations, file, content, DirectBrokerOperationPattern, "direct broker operation");

            if (ReferencesBrokerTransport(content))
            {
                AddForbiddenMatches(violations, file, content, BrokerMethodPattern, "direct broker operation");
            }
        }

        await Assert.That(violations).IsEmpty()
            .Because("API controllers may dispatch MediatR requests or run safe config checks, but must not perform SMTP sends or broker operations directly.");
    }

    private static IEnumerable<string> EnumerateHandlerSourceFiles(string root)
    {
        return EnumerateSourceFiles(root, "*.cs")
            .Where(path => path.Replace(Path.DirectorySeparatorChar, '/')
                .Contains("/Handlers/", StringComparison.Ordinal));
    }

    private static IEnumerable<string> EnumerateSourceFiles(string root, string searchPattern)
    {
        if (!Directory.Exists(root))
        {
            yield break;
        }

        foreach (string file in Directory.GetFiles(root, searchPattern, SearchOption.AllDirectories)
                     .Where(path => !IsGeneratedOrBuildOutput(path))
                     .OrderBy(path => path, StringComparer.Ordinal))
        {
            yield return file;
        }
    }

    private static void AddForbiddenMatches(
        ICollection<string> violations,
        string file,
        string content,
        Regex pattern,
        string description)
    {
        foreach (Match match in pattern.Matches(content))
        {
            violations.Add($"{GetRelativePath(FindRepoRoot(), file)}:{GetLineNumber(content, match.Index)} contains {description}: '{match.Value.Trim()}'.");
        }
    }

    private static bool ReferencesEmailTransport(string content)
    {
        return content.Contains("IEmailService", StringComparison.Ordinal)
            || content.Contains("EmailMessage", StringComparison.Ordinal)
            || content.Contains("System.Net.Mail", StringComparison.Ordinal)
            || content.Contains("SmtpClient", StringComparison.Ordinal)
            || content.Contains("MailKit", StringComparison.Ordinal)
            || content.Contains("MimeKit", StringComparison.Ordinal);
    }

    private static bool ReferencesBrokerTransport(string content)
    {
        return content.Contains("IMessagingProvider", StringComparison.Ordinal)
            || content.Contains("RabbitMqMessagingProvider", StringComparison.Ordinal)
            || content.Contains("RabbitMQ.Client", StringComparison.Ordinal)
            || content.Contains("MQContract.RabbitMQ", StringComparison.Ordinal);
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Explore.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root containing Explore.sln.");
    }

    private static bool IsGeneratedOrBuildOutput(string path)
    {
        string normalized = path.Replace(Path.DirectorySeparatorChar, '/');

        return normalized.Contains("/bin/", StringComparison.Ordinal)
            || normalized.Contains("/obj/", StringComparison.Ordinal)
            || normalized.EndsWith(".g.cs", StringComparison.Ordinal)
            || normalized.EndsWith(".Designer.cs", StringComparison.Ordinal);
    }

    private static string GetRelativePath(string repoRoot, string path)
    {
        return Path.GetRelativePath(repoRoot, path).Replace(Path.DirectorySeparatorChar, '/');
    }

    private static int GetLineNumber(string content, int index)
    {
        return content[..index].Count(character => character == '\n') + 1;
    }
}
