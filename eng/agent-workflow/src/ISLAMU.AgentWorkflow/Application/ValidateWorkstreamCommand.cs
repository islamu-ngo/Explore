// ABOUTME: Validates workstream authority, artifact freshness, packets, paths, transitions, and Git HEAD.
// ABOUTME: Coordinates domain policy through narrow YAML and repository infrastructure ports.

using System.Security.Cryptography;
using System.Text;
using ISLAMU.AgentWorkflow.Domain;

namespace ISLAMU.AgentWorkflow.Application;

public interface IWorkstreamStore
{
    WorkstreamLoadResult Load(string manifestPath, string schemaPath);
}

public interface IRepositoryHeadReader
{
    string? ReadHead(string repositoryPath);
}

public sealed record WorkstreamLoadResult(WorkstreamExecution? Execution, WorkstreamError? Error)
{
    public static WorkstreamLoadResult Success(WorkstreamExecution execution) => new(execution, null);

    public static WorkstreamLoadResult Failure(WorkstreamErrorCode code) => new(null, new WorkstreamError(code));
}

public sealed class ValidateWorkstreamCommand(IWorkstreamStore store, IRepositoryHeadReader headReader)
{
    private const long MaximumArtifactBytes = 16L * 1024 * 1024;
    private const int ArtifactReadBufferBytes = 81920;

    public WorkstreamResult Execute(string manifestPath, string schemaPath, string repositoryPath)
    {
        WorkstreamLoadResult loaded = store.Load(manifestPath, schemaPath);
        if (loaded.Error is not null)
        {
            return WorkstreamResult.Failure(loaded.Error.Code);
        }

        WorkstreamExecution execution = loaded.Execution!;
        if (!string.Equals(execution.SchemaVersion, "workstream.v1", StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(execution.WorkstreamId))
        {
            return WorkstreamResult.Failure(WorkstreamErrorCode.UnknownField);
        }

        if (execution.Artifacts is null || execution.Approvals is null || execution.CurrentPhase is null)
        {
            return WorkstreamResult.Failure(WorkstreamErrorCode.UnknownField);
        }

        foreach (ArtifactBinding artifact in execution.Artifacts.InRevisionOrder())
        {
            ArtifactCheck check = ValidateArtifact(repositoryPath, artifact);
            if (check == ArtifactCheck.Unsafe)
            {
                return WorkstreamResult.Failure(WorkstreamErrorCode.UnsafePath);
            }

            if (check == ArtifactCheck.Stale)
            {
                return WorkstreamResult.Failure(WorkstreamErrorCode.StaleArtifactDigest);
            }
        }

        string expectedRevisionDigest = ComputeRevisionDigest(execution.Artifacts);
        if (!string.Equals(expectedRevisionDigest, execution.RevisionDigest, StringComparison.Ordinal) ||
            !IsApproved(execution.Approvals.Cto, expectedRevisionDigest) ||
            !IsApproved(execution.Approvals.UserImplementation, expectedRevisionDigest))
        {
            return WorkstreamResult.Failure(WorkstreamErrorCode.StaleArtifactDigest);
        }

        if (execution.PhaseCommit is null ||
            !string.Equals(execution.PhaseCommit.Decision, "approved", StringComparison.Ordinal) ||
            !string.Equals(execution.PhaseCommit.PhaseId, execution.CurrentPhase.Id, StringComparison.Ordinal) ||
            !string.Equals(execution.PhaseCommit.RevisionDigest, expectedRevisionDigest, StringComparison.Ordinal) ||
            !string.Equals(execution.PhaseCommit.ExpectedHead, execution.ExpectedHead, StringComparison.Ordinal))
        {
            return WorkstreamResult.Failure(WorkstreamErrorCode.CommitAuthorityRequired);
        }

        WorkstreamError? transitionError = execution.ValidateRequestedTransition();
        if (transitionError is not null)
        {
            return WorkstreamResult.Failure(transitionError.Code);
        }

        PhasePacket packet = execution.CurrentPhase.Packet;
        if (packet is null || packet.Paths is null || packet.Paths.Count == 0 ||
            packet.VerificationCommands is null || packet.VerificationCommands.Count == 0 ||
            packet.Commit is null || string.IsNullOrWhiteSpace(packet.Commit.Type) ||
            string.IsNullOrWhiteSpace(packet.Commit.Scope) ||
            packet.Commit.Changelog is not ("skip" or "required") ||
            packet.Commit.Trailers is null || packet.Commit.Trailers.Count == 0 ||
            packet.VerificationCommands.Any(string.IsNullOrWhiteSpace))
        {
            return WorkstreamResult.Failure(WorkstreamErrorCode.PhasePacketIncomplete);
        }

        if (packet.Paths.Any(path => !IsSafeLiteralPath(path)) || packet.Paths.Distinct(StringComparer.Ordinal).Count() != packet.Paths.Count)
        {
            return WorkstreamResult.Failure(WorkstreamErrorCode.UnsafePath);
        }

        string? actualHead = headReader.ReadHead(repositoryPath);
        if (actualHead is null || !string.Equals(actualHead, execution.ExpectedHead, StringComparison.Ordinal))
        {
            return WorkstreamResult.Failure(WorkstreamErrorCode.ExpectedHeadMismatch);
        }

        return WorkstreamResult.Success(new ValidatedWorkstream(
            execution.WorkstreamId,
            execution.CurrentPhase.Id,
            execution.CurrentPhase.State,
            execution.CurrentPhase.RequestedTransition,
            packet.Paths,
            packet.VerificationCommands));
    }

    private static ArtifactCheck ValidateArtifact(string repositoryPath, ArtifactBinding artifact)
    {
        if (!IsSafeLiteralPath(artifact.Path))
        {
            return ArtifactCheck.Unsafe;
        }

        string? artifactPath = ResolveContainedPath(repositoryPath, artifact.Path);
        if (artifactPath is null || !IsSha256(artifact.Sha256))
        {
            return ArtifactCheck.Stale;
        }

        ArtifactCheck pathCheck = CheckArtifactPathSegments(repositoryPath, artifact.Path);
        if (pathCheck != ArtifactCheck.Valid)
        {
            return pathCheck;
        }

        long metadataLength;
        try
        {
            metadataLength = new FileInfo(artifactPath).Length;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return ArtifactCheck.Stale;
        }

        if (metadataLength < 0)
        {
            return ArtifactCheck.Unsafe;
        }

        if (metadataLength > MaximumArtifactBytes)
        {
            return ArtifactCheck.Stale;
        }

        FileAccess access = OperatingSystem.IsLinux() && metadataLength == 0
            ? FileAccess.ReadWrite
            : FileAccess.Read;
        try
        {
            using Microsoft.Win32.SafeHandles.SafeFileHandle handle = File.OpenHandle(
                artifactPath,
                FileMode.Open,
                access,
                FileShare.Read,
                FileOptions.Asynchronous);
            return ValidateOpenArtifactHandle(handle, repositoryPath, artifact);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return ArtifactCheck.Stale;
        }
    }

    private static ArtifactCheck ValidateOpenArtifactHandle(
        Microsoft.Win32.SafeHandles.SafeFileHandle handle,
        string repositoryPath,
        ArtifactBinding artifact)
    {
        long handleLength;
        try
        {
            handleLength = RandomAccess.GetLength(handle);
        }
        catch (Exception exception) when (exception is IOException or NotSupportedException or ArgumentException)
        {
            return ArtifactCheck.Unsafe;
        }

        if (handleLength < 0)
        {
            return ArtifactCheck.Unsafe;
        }

        if (handleLength > MaximumArtifactBytes)
        {
            return ArtifactCheck.Stale;
        }

        ArtifactCheck pathCheck = CheckArtifactPathSegments(repositoryPath, artifact.Path);
        if (pathCheck != ArtifactCheck.Valid)
        {
            return pathCheck;
        }

        ArtifactCheck hashCheck = HashBoundedRegularFile(handle, artifact.Sha256);
        if (hashCheck != ArtifactCheck.Valid)
        {
            return hashCheck;
        }

        pathCheck = CheckArtifactPathSegments(repositoryPath, artifact.Path);
        return pathCheck == ArtifactCheck.Valid ? ArtifactCheck.Valid : pathCheck;
    }

    private static ArtifactCheck HashBoundedRegularFile(
        Microsoft.Win32.SafeHandles.SafeFileHandle handle,
        string expectedDigest)
    {
        var buffer = new byte[ArtifactReadBufferBytes];
        long totalBytes = 0;
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

        try
        {
            while (true)
            {
                int read = RandomAccess.Read(handle, buffer, totalBytes);
                if (read == 0)
                {
                    break;
                }

                totalBytes = checked(totalBytes + read);
                if (totalBytes > MaximumArtifactBytes)
                {
                    return ArtifactCheck.Stale;
                }

                hash.AppendData(buffer, 0, read);
            }

            long finalLength = RandomAccess.GetLength(handle);
            if (finalLength != totalBytes || finalLength > MaximumArtifactBytes)
            {
                return ArtifactCheck.Stale;
            }
        }
        catch (OverflowException)
        {
            return ArtifactCheck.Stale;
        }
        catch (Exception exception) when (exception is IOException or NotSupportedException or ArgumentException)
        {
            return ArtifactCheck.Unsafe;
        }

        string digest = Convert.ToHexStringLower(hash.GetHashAndReset());
        return string.Equals(digest, expectedDigest, StringComparison.Ordinal)
            ? ArtifactCheck.Valid
            : ArtifactCheck.Stale;
    }

    private static ArtifactCheck CheckArtifactPathSegments(string repositoryPath, string relativePath)
    {
        string current = Path.GetFullPath(repositoryPath);
        string[] segments = relativePath.Split('/', StringSplitOptions.None);
        for (int index = 0; index < segments.Length; index++)
        {
            current = Path.Combine(current, segments[index]);
            try
            {
                FileAttributes attributes = File.GetAttributes(current);
                bool isDirectory = (attributes & FileAttributes.Directory) != 0;
                if ((attributes & (FileAttributes.Device | FileAttributes.ReparsePoint | FileAttributes.Offline)) != 0 ||
                    index < segments.Length - 1 && !isDirectory ||
                    index == segments.Length - 1 && isDirectory)
                {
                    return ArtifactCheck.Unsafe;
                }
            }
            catch (FileNotFoundException)
            {
                return ArtifactCheck.Stale;
            }
            catch (DirectoryNotFoundException)
            {
                return ArtifactCheck.Stale;
            }
            catch (UnauthorizedAccessException)
            {
                return ArtifactCheck.Stale;
            }
            catch (IOException)
            {
                return ArtifactCheck.Stale;
            }
        }

        return ArtifactCheck.Valid;
    }

    private static bool IsApproved(ApprovalBinding approval, string revisionDigest) =>
        approval is not null &&
        string.Equals(approval.Decision, "approved", StringComparison.Ordinal) &&
        string.Equals(approval.RevisionDigest, revisionDigest, StringComparison.Ordinal);

    private static string ComputeRevisionDigest(ArtifactBindings artifacts)
    {
        string value = string.Join('\n', artifacts.InRevisionOrder().Select(artifact => artifact.Sha256));
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }

    private static bool IsSha256(string value) =>
        value is not null && value.Length == 64 && value.All(character => character is >= '0' and <= '9' or >= 'a' and <= 'f');

    private static bool IsSafeLiteralPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || IsWindowsDrivePrefixed(path) || Path.IsPathRooted(path) || path.Contains('\\') || path.IndexOfAny(['*', '?', '[', ']', '\0']) >= 0)
        {
            return false;
        }

        string[] segments = path.Split('/', StringSplitOptions.None);
        return segments.All(segment => segment.Length > 0 && segment is not "." and not "..");
    }

    private static bool IsWindowsDrivePrefixed(string path) =>
        path.Length >= 2 && char.IsAsciiLetter(path[0]) && path[1] == ':';

    private enum ArtifactCheck
    {
        Valid,
        Stale,
        Unsafe,
    }

    private static string? ResolveContainedPath(string repositoryPath, string relativePath)
    {
        try
        {
            string root = Path.GetFullPath(repositoryPath).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string candidate = Path.GetFullPath(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
            return candidate.StartsWith(root, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)
                ? candidate
                : null;
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null;
        }
    }
}
