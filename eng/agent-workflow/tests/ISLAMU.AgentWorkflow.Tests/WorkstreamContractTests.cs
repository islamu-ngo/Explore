// ABOUTME: Specifies revision-bound workstream validation through the future CLI and schema file seams.
// ABOUTME: Proves approvals, transitions, packets, paths, and Git HEAD checks fail closed with typed JSON diagnostics.

using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ISLAMU.AgentWorkflow.Tests;

[NotInParallel("AgentWorkflowBlackBoxProcess")]
public sealed class WorkstreamContractTests
{
    private const string OutputSchema = "workstream-validation.v1";

    [Test]
    public async Task ValidateWorkstreamWithCurrentApprovalReturnsOneLegalTransitionAndPacket()
    {
        using ScenarioFixture fixture = await ScenarioFixture.CreateAsync(Scenario.Valid);
        ProcessResult result = await fixture.InvokeValidatorAsync();
        using JsonDocument document = ParseContractOutput(result);
        JsonElement root = document.RootElement;

        await Assert.That(result.ExitCode).IsEqualTo(0);
        await Assert.That(root.EnumerateObject().Select(property => property.Name)).IsEquivalentTo(
            ["schemaVersion", "ok", "code", "workstreamId", "phaseId", "currentState", "nextTransition", "packet"]);
        await Assert.That(root.GetProperty("schemaVersion").GetString()).IsEqualTo(OutputSchema);
        await Assert.That(root.GetProperty("ok").GetBoolean()).IsTrue();
        await Assert.That(root.GetProperty("code").GetString()).IsEqualTo("workstream_valid");
        await Assert.That(root.GetProperty("workstreamId").GetString()).IsEqualTo("agentic-workflow-control-plane");
        await Assert.That(root.GetProperty("phaseId").GetString()).IsEqualTo("phase-1");
        await Assert.That(root.GetProperty("currentState").GetString()).IsEqualTo("approved");
        await Assert.That(root.GetProperty("nextTransition").GetString()).IsEqualTo("implementing");

        JsonElement packet = root.GetProperty("packet");
        await Assert.That(packet.EnumerateObject().Select(property => property.Name)).IsEquivalentTo(
            ["paths", "verificationCommands"]);
        await Assert.That(packet.GetProperty("paths").EnumerateArray().Select(item => item.GetString()!)).IsEquivalentTo(
            ["eng/agent-workflow/tests/ISLAMU.AgentWorkflow.Tests/WorkstreamContractTests.cs"]);
        await Assert.That(packet.GetProperty("verificationCommands").EnumerateArray().Select(item => item.GetString()!)).IsEquivalentTo(
            ["dotnet test --project eng/agent-workflow/tests/ISLAMU.AgentWorkflow.Tests/ISLAMU.AgentWorkflow.Tests.csproj --configuration Release --verbosity quiet"]);
    }

    [Test]
    public async Task ValidateWorkstreamWithStaleArtifactDigestReturnsTypedFailure()
    {
        await AssertFailureAsync(Scenario.StaleDigest, "stale_artifact_digest");
        await AssertFailureAsync(Scenario.OversizedArtifact, "stale_artifact_digest");
    }

    [Test]
    public async Task ValidateWorkstreamWithMissingPhaseCommitAuthorityReturnsTypedFailure()
    {
        await AssertFailureAsync(Scenario.MissingCommitAuthority, "commit_authority_required");
    }

    [Test]
    public async Task ValidateWorkstreamWithIllegalRequestedTransitionReturnsTypedFailure()
    {
        await AssertFailureAsync(Scenario.IllegalTransition, "illegal_transition");
    }

    [Test]
    public async Task ValidateWorkstreamWithIncompletePhasePacketReturnsTypedFailure()
    {
        await AssertFailureAsync(Scenario.IncompletePacket, "phase_packet_incomplete");
    }

    [Test]
    public async Task ValidateWorkstreamWithUnknownManifestFieldReturnsTypedFailure()
    {
        await AssertFailureAsync(Scenario.UnknownField, "unknown_field");
        await AssertFailureAsync(Scenario.TamperedSchema, "unknown_field");
        await AssertFailureAsync(Scenario.OversizedManifest, "unknown_field");
        await AssertFailureAsync(Scenario.OversizedSchema, "unknown_field");
    }

    [Test]
    public async Task ValidateWorkstreamWithUnsafePacketPathReturnsTypedFailure()
    {
        await AssertFailureAsync(Scenario.UnsafeTraversalPath, "unsafe_path");
        await AssertFailureAsync(Scenario.UnsafeWindowsDrivePath, "unsafe_path");
        await AssertFailureAsync(Scenario.SymlinkArtifact, "unsafe_path");
        await AssertFailureAsync(Scenario.NonRegularArtifact, "unsafe_path");
    }

    [Test]
    public async Task ValidateWorkstreamWhenExpectedHeadDoesNotMatchRepositoryReturnsTypedFailure()
    {
        await AssertFailureAsync(Scenario.ExpectedHeadMismatch, "expected_head_mismatch");
    }

    private static async Task AssertFailureAsync(Scenario scenario, string expectedCode)
    {
        using ScenarioFixture fixture = await ScenarioFixture.CreateAsync(scenario);
        ProcessResult result = await fixture.InvokeValidatorAsync();
        using JsonDocument document = ParseContractOutput(result);
        JsonElement root = document.RootElement;

        await Assert.That(result.ExitCode).IsEqualTo(2);
        await Assert.That(root.EnumerateObject().Select(property => property.Name)).IsEquivalentTo(
            ["schemaVersion", "ok", "code"]);
        await Assert.That(root.GetProperty("schemaVersion").GetString()).IsEqualTo(OutputSchema);
        await Assert.That(root.GetProperty("ok").GetBoolean()).IsFalse();
        await Assert.That(root.GetProperty("code").GetString()).IsEqualTo(expectedCode);
    }

    private static JsonDocument ParseContractOutput(ProcessResult result)
    {
        if (result.StandardOutputTruncated || result.StandardErrorTruncated)
        {
            throw new InvalidOperationException($"process_output_truncated:{TruncatedStreams(result)}");
        }

        if (string.IsNullOrWhiteSpace(result.StandardOutput))
        {
            string diagnostic = FirstNonEmptyLine(result.StandardError) ?? "missing_json_output";
            throw new InvalidOperationException(
                $"Production CLI/schema contract unavailable (exit {result.ExitCode}): {diagnostic}");
        }

        try
        {
            return JsonDocument.Parse(result.StandardOutput, new JsonDocumentOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow,
            });
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(
                $"Production CLI returned non-JSON output (exit {result.ExitCode}): {FirstNonEmptyLine(result.StandardOutput)}",
                exception);
        }
    }

    private static string? FirstNonEmptyLine(string value) =>
        value.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();

    private static string TruncatedStreams(ProcessResult result) =>
        result.StandardOutputTruncated && result.StandardErrorTruncated
            ? "stdout,stderr"
            : result.StandardOutputTruncated ? "stdout" : "stderr";

    private enum Scenario
    {
        Valid,
        StaleDigest,
        OversizedArtifact,
        MissingCommitAuthority,
        IllegalTransition,
        IncompletePacket,
        UnknownField,
        TamperedSchema,
        OversizedManifest,
        OversizedSchema,
        UnsafeTraversalPath,
        UnsafeWindowsDrivePath,
        SymlinkArtifact,
        NonRegularArtifact,
        ExpectedHeadMismatch,
    }

    private sealed class ScenarioFixture : IDisposable
    {
        private const int MaximumOutputCharacters = 65_536;
        private const int MaximumManifestBytes = 1024 * 1024;
        private const int MaximumSchemaBytes = 256 * 1024;
        private const int MaximumArtifactBytes = 16 * 1024 * 1024;
        private static readonly TimeSpan ProcessTimeout = TimeSpan.FromSeconds(30);
        private readonly string repositoryRoot;
        private readonly string manifestPath;
        private readonly string schemaPath;
        private readonly string? externalArtifactPath;
        private bool disposed;

        private ScenarioFixture(string repositoryRoot, string manifestPath, string schemaPath, string? externalArtifactPath)
        {
            this.repositoryRoot = repositoryRoot;
            this.manifestPath = manifestPath;
            this.schemaPath = schemaPath;
            this.externalArtifactPath = externalArtifactPath;
        }

        public static async Task<ScenarioFixture> CreateAsync(Scenario scenario)
        {
            string repositoryRoot = Path.Combine(Path.GetTempPath(), $"islamu-agent-workflow-contract-{Guid.NewGuid():N}");
            string? externalArtifactPath = null;
            Directory.CreateDirectory(repositoryRoot);

            try
            {
                string artifactsRoot = Path.Combine(repositoryRoot, "artifacts");
                Directory.CreateDirectory(artifactsRoot);
                WriteArtifact(artifactsRoot, "plan.md", "plan-revision-v1\n");
                WriteArtifact(artifactsRoot, "tasks.md", "tasks-revision-v1\n");
                WriteArtifact(artifactsRoot, "ivsd.md", "ivsd-revision-v1\n");
                WriteArtifact(artifactsRoot, "cto-review.md", "cto-review-revision-v1\n");

                await RunRequiredProcessAsync("git", repositoryRoot, "init", "--initial-branch=develop");
                await RunRequiredProcessAsync("git", repositoryRoot, "add", "artifacts/plan.md", "artifacts/tasks.md", "artifacts/ivsd.md", "artifacts/cto-review.md");
                await RunRequiredProcessAsync(
                    "git",
                    repositoryRoot,
                    "-c", "user.name=Agent Workflow Contract",
                    "-c", "user.email=agent-workflow@example.invalid",
                    "commit", "-m", "seed deterministic workstream artifacts");
                ProcessResult headResult = await RunRequiredProcessAsync("git", repositoryRoot, "rev-parse", "HEAD");
                string actualHead = headResult.StandardOutput.Trim();

                string planPath = Path.Combine(artifactsRoot, "plan.md");
                if (scenario == Scenario.OversizedArtifact)
                {
                    using var artifact = new FileStream(planPath, FileMode.Create, FileAccess.Write, FileShare.None);
                    artifact.SetLength(MaximumArtifactBytes + 1L);
                }
                else if (scenario == Scenario.SymlinkArtifact)
                {
                    externalArtifactPath = Path.Combine(Path.GetTempPath(), $"islamu-agent-workflow-external-{Guid.NewGuid():N}.md");
                    File.WriteAllText(externalArtifactPath, "plan-revision-v1\n", new UTF8Encoding(false));
                    File.Delete(planPath);
                    try
                    {
                        File.CreateSymbolicLink(planPath, externalArtifactPath);
                    }
                    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or PlatformNotSupportedException)
                    {
                        throw new InvalidOperationException($"symbolic_link_creation_required:{exception.GetType().Name}", exception);
                    }
                }
                else if (scenario == Scenario.NonRegularArtifact)
                {
                    File.Delete(planPath);
                    if (OperatingSystem.IsLinux())
                    {
                        byte[] fifoPath = Encoding.UTF8.GetBytes(planPath + '\0');
                        if (CreateFifo(fifoPath, 384) != 0)
                        {
                            throw new InvalidOperationException(
                                $"fifo_creation_required:{System.Runtime.InteropServices.Marshal.GetLastPInvokeError()}");
                        }
                    }
                    else
                    {
                        Directory.CreateDirectory(planPath);
                    }
                }

                string checkoutRoot = FindCheckoutRoot();
                string approvedSchemaPath = Path.Combine(checkoutRoot, ".agents", "contract", "workstream.schema.json");
                string schemaPath = approvedSchemaPath;
                if (scenario is Scenario.TamperedSchema or Scenario.OversizedSchema)
                {
                    schemaPath = Path.Combine(repositoryRoot, "workstream.schema.json");
                    string schema = File.ReadAllText(approvedSchemaPath);
                    if (scenario == Scenario.TamperedSchema)
                    {
                        schema = schema.Replace(
                            "\"title\": \"Agent Workstream Execution\"",
                            "\"title\": \"Tampered Agent Workstream Execution\"",
                            StringComparison.Ordinal);
                    }
                    else
                    {
                        schema += new string(' ', MaximumSchemaBytes);
                    }

                    File.WriteAllText(schemaPath, schema, new UTF8Encoding(false));
                }

                string manifestPath = Path.Combine(repositoryRoot, "workstream.yaml");
                File.WriteAllText(manifestPath, BuildManifest(repositoryRoot, actualHead, scenario), new UTF8Encoding(false));
                if (scenario == Scenario.OversizedManifest)
                {
                    File.AppendAllText(manifestPath, "\n#" + new string('x', MaximumManifestBytes), new UTF8Encoding(false));
                }

                return new ScenarioFixture(repositoryRoot, manifestPath, schemaPath, externalArtifactPath);
            }
            catch
            {
                Directory.Delete(repositoryRoot, recursive: true);
                if (externalArtifactPath is not null)
                {
                    File.Delete(externalArtifactPath);
                }

                throw;
            }
        }

        public async Task<ProcessResult> InvokeValidatorAsync()
        {
            string checkoutRoot = FindCheckoutRoot();
            string projectPath = Path.Combine(
                checkoutRoot,
                "eng", "agent-workflow", "src", "ISLAMU.AgentWorkflow", "ISLAMU.AgentWorkflow.csproj");

            return await RunProcessAsync(
                "dotnet",
                checkoutRoot,
                "run",
                "--project", projectPath,
                "--configuration", "Release",
                "--verbosity", "quiet",
                "--",
                "validate-workstream",
                "--manifest", manifestPath,
                "--schema", schemaPath,
                "--repository", repositoryRoot,
                "--output", "json");
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            Directory.Delete(repositoryRoot, recursive: true);
            if (externalArtifactPath is not null)
            {
                File.Delete(externalArtifactPath);
            }
        }

        private static string BuildManifest(string repositoryRoot, string actualHead, Scenario scenario)
        {
            string planDigest = scenario == Scenario.NonRegularArtifact
                ? DigestText("non-regular-artifact")
                : Digest(Path.Combine(repositoryRoot, "artifacts", "plan.md"));
            string tasksDigest = Digest(Path.Combine(repositoryRoot, "artifacts", "tasks.md"));
            string ivsdDigest = Digest(Path.Combine(repositoryRoot, "artifacts", "ivsd.md"));
            string ctoDigest = Digest(Path.Combine(repositoryRoot, "artifacts", "cto-review.md"));
            string revisionDigest = DigestText(string.Join('\n', planDigest, tasksDigest, ivsdDigest, ctoDigest));

            if (scenario == Scenario.StaleDigest)
            {
                planDigest = new string('0', 64);
            }

            string expectedHead = scenario == Scenario.ExpectedHeadMismatch ? new string('f', 40) : actualHead;
            string requestedTransition = scenario == Scenario.IllegalTransition ? "committed" : "implementing";
            string packetPath = scenario switch
            {
                Scenario.UnsafeTraversalPath => "../foreign-contributor.txt",
                Scenario.UnsafeWindowsDrivePath => "C:/foreign/work.md",
                _ => "eng/agent-workflow/tests/ISLAMU.AgentWorkflow.Tests/WorkstreamContractTests.cs",
            };
            string unknownField = scenario == Scenario.UnknownField ? "unexpectedAuthority: executor\n" : string.Empty;
            string commitAuthority = scenario == Scenario.MissingCommitAuthority
                ? string.Empty
                : $$"""
                  phaseCommit:
                    decision: approved
                    phaseId: phase-1
                    revisionDigest: {{revisionDigest}}
                    expectedHead: {{expectedHead}}
                """ + "\n";
            string verificationCommands = scenario == Scenario.IncompletePacket
                ? string.Empty
                : "    verificationCommands:\n      - dotnet test --project eng/agent-workflow/tests/ISLAMU.AgentWorkflow.Tests/ISLAMU.AgentWorkflow.Tests.csproj --configuration Release --verbosity quiet\n";

            return $$"""
                schemaVersion: workstream.v1
                workstreamId: agentic-workflow-control-plane
                {{unknownField}}artifacts:
                  plan:
                    path: artifacts/plan.md
                    sha256: {{planDigest}}
                  tasks:
                    path: artifacts/tasks.md
                    sha256: {{tasksDigest}}
                  ivsd:
                    path: artifacts/ivsd.md
                    sha256: {{ivsdDigest}}
                  ctoReview:
                    path: artifacts/cto-review.md
                    sha256: {{ctoDigest}}
                revisionDigest: {{revisionDigest}}
                approvals:
                  cto:
                    decision: approved
                    revisionDigest: {{revisionDigest}}
                  userImplementation:
                    decision: approved
                    revisionDigest: {{revisionDigest}}
                {{commitAuthority}}expectedHead: {{expectedHead}}
                currentPhase:
                  id: phase-1
                  state: approved
                  requestedTransition: {{requestedTransition}}
                  packet:
                    paths:
                      - {{packetPath}}
                {{verificationCommands}}    commit:
                      type: build
                      scope: architecture
                      changelog: skip
                      trailers:
                        - "Changelog: skip"
                """;
        }

        [System.Runtime.InteropServices.DllImport("libc", SetLastError = true, EntryPoint = "mkfifo")]
        private static extern int CreateFifo(byte[] pathname, uint mode);

        private static void WriteArtifact(string root, string fileName, string content) =>
            File.WriteAllText(Path.Combine(root, fileName), content, new UTF8Encoding(false));

        private static string Digest(string path)
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            return Convert.ToHexStringLower(SHA256.HashData(stream));
        }

        private static string DigestText(string value) =>
            Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

        private static string FindCheckoutRoot()
        {
            DirectoryInfo? directory = new(AppContext.BaseDirectory);
            while (directory is not null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "Directory.Packages.props")) &&
                    Directory.Exists(Path.Combine(directory.FullName, ".git")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new InvalidOperationException("repository_root_not_found");
        }

        private static async Task<ProcessResult> RunRequiredProcessAsync(
            string executable,
            string workingDirectory,
            params string[] arguments)
        {
            ProcessResult result = await RunProcessAsync(executable, workingDirectory, arguments);
            if (result.StandardOutputTruncated || result.StandardErrorTruncated)
            {
                throw new InvalidOperationException(
                    $"test_fixture_process_output_truncated:{executable}:{TruncatedStreams(result)}");
            }

            if (result.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"test_fixture_process_failed:{executable}:{FirstNonEmptyLine(result.StandardError) ?? result.ExitCode.ToString(CultureInfo.InvariantCulture)}");
            }

            return result;
        }

        private static async Task<ProcessResult> RunProcessAsync(
            string executable,
            string workingDirectory,
            params string[] arguments)
        {
            var startInfo = new ProcessStartInfo(executable)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                WorkingDirectory = workingDirectory,
            };
            foreach (string argument in arguments)
            {
                startInfo.ArgumentList.Add(argument);
            }

            if (string.Equals(executable, "git", StringComparison.Ordinal))
            {
                string nullDevice = OperatingSystem.IsWindows() ? "NUL" : "/dev/null";
                startInfo.Environment["GIT_CONFIG_NOSYSTEM"] = "1";
                startInfo.Environment["GIT_CONFIG_GLOBAL"] = nullDevice;
                startInfo.Environment["GIT_AUTHOR_DATE"] = "2026-09-01T00:00:00Z";
                startInfo.Environment["GIT_COMMITTER_DATE"] = "2026-09-01T00:00:00Z";
            }

            using var process = new Process { StartInfo = startInfo };
            process.Start();
            Task<StreamCapture> standardOutputDrain = DrainAsync(process.StandardOutput);
            Task<StreamCapture> standardErrorDrain = DrainAsync(process.StandardError);
            using var timeout = new CancellationTokenSource(ProcessTimeout);

            try
            {
                await process.WaitForExitAsync(timeout.Token);
            }
            catch (OperationCanceledException) when (timeout.IsCancellationRequested)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
                await Task.WhenAll(standardOutputDrain, standardErrorDrain);
                throw new TimeoutException($"process_timeout:{executable}");
            }

            StreamCapture output = await standardOutputDrain;
            StreamCapture error = await standardErrorDrain;
            return new ProcessResult(
                process.ExitCode,
                output.Value,
                error.Value,
                output.Truncated,
                error.Truncated);
        }

        private static async Task<StreamCapture> DrainAsync(TextReader reader)
        {
            const int BufferCharacters = 4_096;
            var retained = new StringBuilder(capacity: MaximumOutputCharacters);
            var buffer = new char[BufferCharacters];
            bool truncated = false;
            int read;

            while ((read = await reader.ReadAsync(buffer.AsMemory())) != 0)
            {
                int remaining = MaximumOutputCharacters - retained.Length;
                int retainedCharacters = Math.Min(read, remaining);
                if (retainedCharacters > 0)
                {
                    retained.Append(buffer, 0, retainedCharacters);
                }

                truncated |= read > retainedCharacters;
            }

            return new StreamCapture(retained.ToString(), truncated);
        }
    }

    private sealed record StreamCapture(string Value, bool Truncated);

    private sealed record ProcessResult(
        int ExitCode,
        string StandardOutput,
        string StandardError,
        bool StandardOutputTruncated,
        bool StandardErrorTruncated);
}
