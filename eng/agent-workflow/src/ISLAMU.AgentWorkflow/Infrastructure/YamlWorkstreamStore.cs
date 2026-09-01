// ABOUTME: Loads canonical, bounded workstream YAML and executes bounded Git HEAD reads without a shell.
// ABOUTME: Rejects aliases, excessive structure, and values outside the approved schema before typed mapping.

using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using ISLAMU.AgentWorkflow.Application;
using ISLAMU.AgentWorkflow.Domain;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ISLAMU.AgentWorkflow.Infrastructure;

public sealed class YamlWorkstreamStore : IWorkstreamStore
{
    private const int MaximumManifestBytes = 1024 * 1024;
    private const int MaximumSchemaBytes = 256 * 1024;
    private const int MaximumYamlDepth = 32;
    private const int MaximumYamlNodes = 4096;
    private const string ApprovedSchemaSha256 = "28e29a19210e5f6c2feabb3095d436a226c5a69c3f113e81e9d1bc809b195b5c";

    private static readonly Regex WorkstreamIdPattern = Pattern("^[a-z][a-z0-9]*(-[a-z0-9]+)*$");
    private static readonly Regex Sha256Pattern = Pattern("^[a-f0-9]{64}$");
    private static readonly Regex GitObjectIdPattern = Pattern("^[a-f0-9]{40,64}$");
    private static readonly Regex PhaseIdPattern = Pattern("^phase-[1-9][0-9]*$");
    private static readonly HashSet<string> States = new(StringComparer.Ordinal)
    {
        "approved", "implementing", "verifying", "commit-ready", "committed", "complete",
        "blocked", "interrupted", "needs-replan",
    };
    private static readonly HashSet<string> RequestedTransitions = new(StringComparer.Ordinal)
    {
        "implementing", "verifying", "commit-ready", "committed", "complete", "blocked",
        "interrupted", "needs-replan",
    };

    private readonly IDeserializer deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    public WorkstreamLoadResult Load(string manifestPath, string schemaPath)
    {
        try
        {
            if (!SchemaIsCanonical(schemaPath) || !TryReadBoundedUtf8(manifestPath, MaximumManifestBytes, out string yaml) ||
                !HasBoundedYamlStructure(yaml))
            {
                return WorkstreamLoadResult.Failure(WorkstreamErrorCode.UnknownField);
            }

            var stream = new YamlStream();
            using (var reader = new StringReader(yaml))
            {
                stream.Load(reader);
            }

            if (stream.Documents.Count != 1)
            {
                return WorkstreamLoadResult.Failure(WorkstreamErrorCode.UnknownField);
            }

            YamlNode root = stream.Documents[0].RootNode;
            WorkstreamErrorCode? typedConstraintError = ClassifyTypedConstraintFailure(root);
            if (typedConstraintError is not null)
            {
                return WorkstreamLoadResult.Failure(typedConstraintError.Value);
            }

            if (!ValidateManifest(root))
            {
                return WorkstreamLoadResult.Failure(WorkstreamErrorCode.UnknownField);
            }

            ManifestDto? manifest = deserializer.Deserialize<ManifestDto>(yaml);
            WorkstreamExecution? execution = Map(manifest);
            return execution is null
                ? WorkstreamLoadResult.Failure(WorkstreamErrorCode.UnknownField)
                : WorkstreamLoadResult.Success(execution);
        }
        catch (Exception exception) when (exception is IOException or YamlException or InvalidOperationException or ArgumentException or UnauthorizedAccessException or DecoderFallbackException)
        {
            return WorkstreamLoadResult.Failure(WorkstreamErrorCode.UnknownField);
        }
    }

    private static bool SchemaIsCanonical(string schemaPath) =>
        TryComputeBoundedSha256(schemaPath, MaximumSchemaBytes, out string digest) &&
        string.Equals(digest, ApprovedSchemaSha256, StringComparison.Ordinal);

    private static bool TryComputeBoundedSha256(string path, int maximumBytes, out string digest)
    {
        digest = string.Empty;
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            var buffer = new byte[81920];
            int total = 0;
            int read;
            while ((read = stream.Read(buffer, 0, buffer.Length)) != 0)
            {
                total = checked(total + read);
                if (total > maximumBytes)
                {
                    return false;
                }

                hash.AppendData(buffer, 0, read);
            }

            digest = Convert.ToHexStringLower(hash.GetHashAndReset());
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return false;
        }
    }

    private static bool TryReadBoundedUtf8(string path, int maximumBytes, out string value)
    {
        value = string.Empty;
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var content = new MemoryStream();
            var buffer = new byte[81920];
            int total = 0;
            int read;
            while ((read = stream.Read(buffer, 0, buffer.Length)) != 0)
            {
                total = checked(total + read);
                if (total > maximumBytes)
                {
                    return false;
                }

                content.Write(buffer, 0, read);
            }

            value = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true)
                .GetString(content.GetBuffer(), 0, checked((int)content.Length));
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException or DecoderFallbackException)
        {
            return false;
        }
    }

    private static bool HasBoundedYamlStructure(string yaml)
    {
        var parser = new Parser(new StringReader(yaml));
        int depth = 0;
        int nodes = 0;

        while (parser.MoveNext())
        {
            ParsingEvent? current = parser.Current;
            if (current is AnchorAlias || current is NodeEvent nodeEvent && !nodeEvent.Anchor.IsEmpty)
            {
                return false;
            }

            if (current is MappingStart or SequenceStart)
            {
                depth++;
                nodes++;
                if (depth > MaximumYamlDepth || nodes > MaximumYamlNodes)
                {
                    return false;
                }
            }
            else if (current is MappingEnd or SequenceEnd)
            {
                depth--;
            }
            else if (current is Scalar)
            {
                nodes++;
                if (nodes > MaximumYamlNodes)
                {
                    return false;
                }
            }
        }

        return depth == 0;
    }

    private static WorkstreamErrorCode? ClassifyTypedConstraintFailure(YamlNode node)
    {
        if (node is not YamlMappingNode root)
        {
            return null;
        }

        if (TryChild(root, "artifacts", out YamlNode artifactsNode) && artifactsNode is YamlMappingNode artifacts)
        {
            foreach (YamlNode artifactNode in artifacts.Children.Values)
            {
                if (artifactNode is YamlMappingNode artifact && TryChild(artifact, "path", out YamlNode pathNode) &&
                    TryScalar(pathNode, out string path) && !IsSafeLiteralPath(path))
                {
                    return WorkstreamErrorCode.UnsafePath;
                }
            }
        }

        if (TryChild(root, "approvals", out YamlNode approvalsNode) && approvalsNode is YamlMappingNode approvals &&
            !TryChild(approvals, "phaseCommit", out _))
        {
            return WorkstreamErrorCode.CommitAuthorityRequired;
        }

        if (!TryChild(root, "currentPhase", out YamlNode phaseNode) || phaseNode is not YamlMappingNode phase ||
            !TryChild(phase, "packet", out YamlNode packetNode) || packetNode is not YamlMappingNode packet)
        {
            return null;
        }

        if (TryChild(packet, "paths", out YamlNode pathsNode) && pathsNode is YamlSequenceNode paths)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (YamlNode pathNode in paths.Children)
            {
                if (TryScalar(pathNode, out string path) && (!IsSafeLiteralPath(path) || !seen.Add(path)))
                {
                    return WorkstreamErrorCode.UnsafePath;
                }
            }
        }

        if (!TryChild(packet, "paths", out pathsNode) || pathsNode is not YamlSequenceNode { Children.Count: > 0 } ||
            !TryChild(packet, "verificationCommands", out YamlNode commandsNode) ||
            !ValidateUniqueSequence(commandsNode, value => value.Length > 0) ||
            !TryChild(packet, "commit", out YamlNode commitNode) || !ValidateCommit(commitNode))
        {
            return WorkstreamErrorCode.PhasePacketIncomplete;
        }

        return null;
    }

    private static bool TryChild(YamlMappingNode mapping, string name, out YamlNode value)
    {
        foreach ((YamlNode keyNode, YamlNode child) in mapping.Children)
        {
            if (TryScalar(keyNode, out string key) && string.Equals(key, name, StringComparison.Ordinal))
            {
                value = child;
                return true;
            }
        }

        value = null!;
        return false;
    }

    private static bool ValidateManifest(YamlNode node)
    {
        if (!TryObject(node, ["schemaVersion", "workstreamId", "artifacts", "revisionDigest", "approvals", "expectedHead", "currentPhase"], out var root) ||
            !ScalarEquals(root["schemaVersion"], "workstream.v1") ||
            !ScalarMatches(root["workstreamId"], WorkstreamIdPattern) ||
            !ScalarMatches(root["revisionDigest"], Sha256Pattern) ||
            !ScalarMatches(root["expectedHead"], GitObjectIdPattern) ||
            !ValidateArtifacts(root["artifacts"]) || !ValidateApprovals(root["approvals"]) ||
            !ValidateCurrentPhase(root["currentPhase"]))
        {
            return false;
        }

        return true;
    }

    private static bool ValidateArtifacts(YamlNode node)
    {
        return TryObject(node, ["plan", "tasks", "ivsd", "ctoReview"], out var artifacts) &&
               artifacts.Values.All(ValidateArtifact);
    }

    private static bool ValidateArtifact(YamlNode node)
    {
        return TryObject(node, ["path", "sha256"], out var artifact) &&
               TryScalar(artifact["path"], out string path) && IsSafeLiteralPath(path) &&
               ScalarMatches(artifact["sha256"], Sha256Pattern);
    }

    private static bool ValidateApprovals(YamlNode node)
    {
        return TryObject(node, ["cto", "userImplementation", "phaseCommit"], out var approvals) &&
               ValidateApproval(approvals["cto"]) && ValidateApproval(approvals["userImplementation"]) &&
               TryObject(approvals["phaseCommit"], ["decision", "phaseId", "revisionDigest", "expectedHead"], out var authority) &&
               ScalarEquals(authority["decision"], "approved") &&
               ScalarMatches(authority["phaseId"], PhaseIdPattern) &&
               ScalarMatches(authority["revisionDigest"], Sha256Pattern) &&
               ScalarMatches(authority["expectedHead"], GitObjectIdPattern);
    }

    private static bool ValidateApproval(YamlNode node)
    {
        return TryObject(node, ["decision", "revisionDigest"], out var approval) &&
               ScalarEquals(approval["decision"], "approved") &&
               ScalarMatches(approval["revisionDigest"], Sha256Pattern);
    }

    private static bool ValidateCurrentPhase(YamlNode node)
    {
        return TryObject(node, ["id", "state", "requestedTransition", "packet"], out var phase) &&
               ScalarMatches(phase["id"], PhaseIdPattern) &&
               TryScalar(phase["state"], out string state) && States.Contains(state) &&
               TryScalar(phase["requestedTransition"], out string transition) && RequestedTransitions.Contains(transition) &&
               ValidatePacket(phase["packet"]);
    }

    private static bool ValidatePacket(YamlNode node)
    {
        return TryObject(node, ["paths", "verificationCommands", "commit"], out var packet) &&
               ValidateUniqueSequence(packet["paths"], IsSafeLiteralPath) &&
               ValidateUniqueSequence(packet["verificationCommands"], value => value.Length > 0) &&
               ValidateCommit(packet["commit"]);
    }

    private static bool ValidateCommit(YamlNode node)
    {
        return TryObject(node, ["type", "scope", "changelog", "trailers"], out var commit) &&
               ScalarHasContent(commit["type"]) && ScalarHasContent(commit["scope"]) &&
               TryScalar(commit["changelog"], out string changelog) && changelog is "skip" or "required" &&
               ValidateUniqueSequence(commit["trailers"], value => value.Length > 0);
    }

    private static bool ValidateUniqueSequence(YamlNode node, Func<string, bool> validate)
    {
        if (node is not YamlSequenceNode { Children.Count: > 0 } sequence)
        {
            return false;
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        return sequence.Children.All(child => TryScalar(child, out string value) && validate(value) && seen.Add(value));
    }

    private static bool TryObject(YamlNode node, string[] requiredFields, out Dictionary<string, YamlNode> fields)
    {
        fields = new Dictionary<string, YamlNode>(StringComparer.Ordinal);
        if (node is not YamlMappingNode mapping || mapping.Children.Count != requiredFields.Length)
        {
            return false;
        }

        var required = new HashSet<string>(requiredFields, StringComparer.Ordinal);
        foreach ((YamlNode keyNode, YamlNode valueNode) in mapping.Children)
        {
            if (!TryScalar(keyNode, out string key) || !required.Contains(key) || !fields.TryAdd(key, valueNode))
            {
                return false;
            }
        }

        return fields.Count == required.Count;
    }

    private static bool TryScalar(YamlNode node, out string value)
    {
        if (node is YamlScalarNode { Value: not null } scalar)
        {
            value = scalar.Value;
            return true;
        }

        value = string.Empty;
        return false;
    }

    private static bool ScalarEquals(YamlNode node, string expected) =>
        TryScalar(node, out string value) && string.Equals(value, expected, StringComparison.Ordinal);

    private static bool ScalarMatches(YamlNode node, Regex pattern) =>
        TryScalar(node, out string value) && pattern.IsMatch(value);

    private static bool ScalarHasContent(YamlNode node) => TryScalar(node, out string value) && value.Length > 0;

    private static bool IsSafeLiteralPath(string path)
    {
        if (path.Length == 0 || Path.IsPathRooted(path) || path.Contains('\\') ||
            path.IndexOfAny(['*', '?', '[', ']', '\0']) >= 0 ||
            path.Length >= 2 && char.IsAsciiLetter(path[0]) && path[1] == ':')
        {
            return false;
        }

        return path.Split('/', StringSplitOptions.None)
            .All(segment => segment.Length > 0 && segment is not "." and not "..");
    }

    private static Regex Pattern(string pattern) =>
        new(pattern, RegexOptions.CultureInvariant | RegexOptions.NonBacktracking, TimeSpan.FromSeconds(1));

    private static WorkstreamExecution? Map(ManifestDto? manifest)
    {
        if (manifest?.Artifacts?.Plan is null || manifest.Artifacts.Tasks is null || manifest.Artifacts.Ivsd is null ||
            manifest.Artifacts.CtoReview is null || manifest.Approvals?.Cto is null || manifest.Approvals.UserImplementation is null ||
            manifest.CurrentPhase?.Packet is null)
        {
            return null;
        }

        static ArtifactBinding Artifact(ArtifactDto value) => new(value.Path ?? string.Empty, value.Sha256 ?? string.Empty);
        static ApprovalBinding Approval(ApprovalDto value) => new(value.Decision ?? string.Empty, value.RevisionDigest ?? string.Empty);

        PhaseCommitAuthority? authority = manifest.Approvals.PhaseCommit is null
            ? null
            : new PhaseCommitAuthority(
                manifest.Approvals.PhaseCommit.Decision ?? string.Empty,
                manifest.Approvals.PhaseCommit.PhaseId ?? string.Empty,
                manifest.Approvals.PhaseCommit.RevisionDigest ?? string.Empty,
                manifest.Approvals.PhaseCommit.ExpectedHead ?? string.Empty);
        CommitPacket? commit = manifest.CurrentPhase.Packet.Commit is null
            ? null
            : new CommitPacket(
                manifest.CurrentPhase.Packet.Commit.Type ?? string.Empty,
                manifest.CurrentPhase.Packet.Commit.Scope ?? string.Empty,
                manifest.CurrentPhase.Packet.Commit.Changelog ?? string.Empty,
                manifest.CurrentPhase.Packet.Commit.Trailers ?? []);

        return new WorkstreamExecution(
            manifest.SchemaVersion ?? string.Empty,
            manifest.WorkstreamId ?? string.Empty,
            new ArtifactBindings(
                Artifact(manifest.Artifacts.Plan),
                Artifact(manifest.Artifacts.Tasks),
                Artifact(manifest.Artifacts.Ivsd),
                Artifact(manifest.Artifacts.CtoReview)),
            manifest.RevisionDigest ?? string.Empty,
            new ApprovalBindings(Approval(manifest.Approvals.Cto), Approval(manifest.Approvals.UserImplementation)),
            authority,
            manifest.ExpectedHead ?? string.Empty,
            new CurrentPhase(
                manifest.CurrentPhase.Id ?? string.Empty,
                manifest.CurrentPhase.State ?? string.Empty,
                manifest.CurrentPhase.RequestedTransition ?? string.Empty,
                new PhasePacket(
                    manifest.CurrentPhase.Packet.Paths ?? [],
                    manifest.CurrentPhase.Packet.VerificationCommands ?? [],
                    commit)));
    }

    private sealed class ManifestDto
    {
        public string? SchemaVersion { get; set; }
        public string? WorkstreamId { get; set; }
        public ArtifactsDto? Artifacts { get; set; }
        public string? RevisionDigest { get; set; }
        public ApprovalsDto? Approvals { get; set; }
        public string? ExpectedHead { get; set; }
        public CurrentPhaseDto? CurrentPhase { get; set; }
    }

    private sealed class ArtifactsDto
    {
        public ArtifactDto? Plan { get; set; }
        public ArtifactDto? Tasks { get; set; }
        public ArtifactDto? Ivsd { get; set; }
        public ArtifactDto? CtoReview { get; set; }
    }

    private sealed class ArtifactDto
    {
        public string? Path { get; set; }
        public string? Sha256 { get; set; }
    }

    private sealed class ApprovalsDto
    {
        public ApprovalDto? Cto { get; set; }
        public ApprovalDto? UserImplementation { get; set; }
        public PhaseCommitDto? PhaseCommit { get; set; }
    }

    private sealed class ApprovalDto
    {
        public string? Decision { get; set; }
        public string? RevisionDigest { get; set; }
    }

    private sealed class PhaseCommitDto
    {
        public string? Decision { get; set; }
        public string? PhaseId { get; set; }
        public string? RevisionDigest { get; set; }
        public string? ExpectedHead { get; set; }
    }

    private sealed class CurrentPhaseDto
    {
        public string? Id { get; set; }
        public string? State { get; set; }
        public string? RequestedTransition { get; set; }
        public PacketDto? Packet { get; set; }
    }

    private sealed class PacketDto
    {
        public List<string>? Paths { get; set; }
        public List<string>? VerificationCommands { get; set; }
        public CommitDto? Commit { get; set; }
    }

    private sealed class CommitDto
    {
        public string? Type { get; set; }
        public string? Scope { get; set; }
        public string? Changelog { get; set; }
        public List<string>? Trailers { get; set; }
    }
}

public sealed class GitHeadReader : IRepositoryHeadReader
{
    private const int MaximumOutputCharacters = 256;

    public string? ReadHead(string repositoryPath)
    {
        if (!Directory.Exists(repositoryPath))
        {
            return null;
        }

        var startInfo = new ProcessStartInfo("git")
        {
            WorkingDirectory = repositoryPath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        startInfo.ArgumentList.Add("rev-parse");
        startInfo.ArgumentList.Add("--verify");
        startInfo.ArgumentList.Add("HEAD");

        try
        {
            using var process = new Process { StartInfo = startInfo };
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            string head = output.Trim();
            return process.ExitCode == 0 && error.Length == 0 && head.Length is >= 40 and <= 64 && output.Length <= MaximumOutputCharacters
                ? head
                : null;
        }
        catch (Exception exception) when (exception is InvalidOperationException or IOException or System.ComponentModel.Win32Exception)
        {
            return null;
        }
    }
}
