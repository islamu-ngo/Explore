// ABOUTME: Exercises provider-neutral Git object validation against disposable synthetic repositories.
// ABOUTME: Covers release-line tag selection, annotated tags, ref safety, and graph integrity failures.

using System.Diagnostics;
using ISLAMU.ReleaseEngineering;

namespace ISLAMU.ReleaseEngineering.Tests;

[NotInParallel]
public sealed class GitRepositoryValidatorTests
{
    [Test]
    public async Task ValidRepositorySelectsOnlyActiveReleaseLineTags()
    {
        using var repo = GitRepositoryFixture.Create();
        string v110 = repo.Commit("v1.1.0");
        repo.AnnotatedTag("v1.1.0", v110);
        string candidate = repo.Commit("v1.1.1 preparation");
        repo.Branch("release/v1.1", candidate);
        repo.Checkout("main");
        string v120 = repo.Commit("v1.2.0");
        repo.AnnotatedTag("v1.2.0", v120);

        GitReleaseValidationResult result = GitRepositoryValidator.Validate(new GitReleaseValidationRequest(
            repo.Path,
            "v1.1",
            "1.1.1",
            "refs/heads/release/v1.1",
            candidate));

        await Assert.That(result.IsValid).IsTrue();
        await Assert.That(result.Identity?.ObjectFormat).IsEqualTo("sha1");
        await Assert.That(result.Identity?.OidLength).IsEqualTo(40);
        await Assert.That(result.Identity?.BaseStableTag).IsEqualTo("v1.1.0");
        await Assert.That(result.Identity?.PreviousPublishedTag).IsEqualTo("v1.1.0");
        await Assert.That(result.Identity?.CandidateOid).IsEqualTo(candidate);
        await Assert.That(result.Identity?.BaseStableCommitOid).IsEqualTo(v110);
    }

    [Test]
    public async Task LightweightReleaseTagsFailClosed()
    {
        using var repo = GitRepositoryFixture.Create();
        string v110 = repo.Commit("v1.1.0");
        repo.LightweightTag("v1.1.0", v110);
        string candidate = repo.Commit("v1.1.1 preparation");
        repo.Branch("release/v1.1", candidate);

        GitReleaseValidationResult result = GitRepositoryValidator.Validate(Request(repo, "1.1.1", candidate));

        await Assert.That(result.Diagnostics).Contains("git_lightweight_tag:v1.1.0");
    }

    [Test]
    public async Task RepositoryStateSafetyFailuresAreStableDiagnostics()
    {
        using var repo = GitRepositoryFixture.Create();
        string v110 = repo.Commit("v1.1.0");
        repo.AnnotatedTag("v1.1.0", v110);
        string candidate = repo.Commit("v1.1.1 preparation");
        repo.Branch("release/v1.1", candidate);
        repo.Replace(v110, candidate);
        repo.WriteGraft(candidate, v110);
        repo.MarkPartialClone();

        GitReleaseValidationResult result = GitRepositoryValidator.Validate(Request(repo, "1.1.1", candidate));

        await Assert.That(result.Diagnostics).Contains("git_replace_refs_present");
        await Assert.That(result.Diagnostics).Contains("git_grafts_present");
        await Assert.That(result.Diagnostics).Contains("git_partial_clone_objects_missing");
    }

    [Test]
    public async Task AmbiguousRefsMissingObjectsAndWrongLineVersionsFailClosed()
    {
        using var repo = GitRepositoryFixture.Create();
        string v110 = repo.Commit("v1.1.0");
        repo.AnnotatedTag("v1.1.0", v110);
        string candidate = repo.Commit("v1.1.1 preparation");
        repo.Branch("release/v1.1", candidate);
        repo.LightweightTag("release/v1.1", candidate);

        GitReleaseValidationResult ambiguous = GitRepositoryValidator.Validate(new GitReleaseValidationRequest(repo.Path, "v1.1", "1.1.1", "release/v1.1", candidate));
        GitReleaseValidationResult wrongLine = GitRepositoryValidator.Validate(new GitReleaseValidationRequest(repo.Path, "v1.1", "1.2.0", "refs/heads/release/v1.1", candidate));
        GitReleaseValidationResult missing = GitRepositoryValidator.Validate(new GitReleaseValidationRequest(repo.Path, "v1.1", "1.1.1", "refs/heads/release/v1.1", new string('f', 40)));

        await Assert.That(ambiguous.Diagnostics).Contains("git_ambiguous_ref:release/v1.1");
        await Assert.That(wrongLine.Diagnostics).Contains("git_selected_version_line_mismatch");
        await Assert.That(missing.Diagnostics).Contains("git_missing_object:candidate");
    }

    [Test]
    public async Task ShallowRepositoriesNonAncestorPreviousAndMovedCandidatesFailClosed()
    {
        using var source = GitRepositoryFixture.Create();
        string v110 = source.Commit("v1.1.0");
        source.AnnotatedTag("v1.1.0", v110);
        string candidate = source.Commit("v1.1.1 preparation");
        source.Branch("release/v1.1", candidate);
        using GitRepositoryFixture shallow = source.CloneDepthOne();

        GitReleaseValidationResult shallowResult = GitRepositoryValidator.Validate(Request(shallow, "1.1.1", candidate));

        using var moved = GitRepositoryFixture.Create();
        string previous = moved.Commit("v1.1.0");
        moved.AnnotatedTag("v1.1.0", previous);
        string oldCandidate = moved.Commit("old preparation");
        string newCandidate = moved.Commit("moved preparation");
        moved.Branch("release/v1.1", newCandidate);
        GitReleaseValidationResult movedResult = GitRepositoryValidator.Validate(Request(moved, "1.1.1", oldCandidate));

        using var unrelated = GitRepositoryFixture.Create();
        string previousUnrelated = unrelated.Commit("v1.1.0");
        unrelated.AnnotatedTag("v1.1.0", previousUnrelated);
        unrelated.Orphan("release work");
        string unrelatedCandidate = unrelated.Commit("v1.1.1 preparation");
        unrelated.Branch("release/v1.1", unrelatedCandidate);
        GitReleaseValidationResult unrelatedResult = GitRepositoryValidator.Validate(Request(unrelated, "1.1.1", unrelatedCandidate));

        await Assert.That(shallowResult.Diagnostics).Contains("git_shallow_repository");
        await Assert.That(movedResult.Diagnostics).Contains("git_candidate_not_release_branch_head");
        await Assert.That(unrelatedResult.Diagnostics).Contains("git_previous_not_ancestor");
    }

    [Test]
    public async Task Sha256RepositoryUsesObservedFullObjectLength()
    {
        using var repo = GitRepositoryFixture.Create("sha256");
        string v110 = repo.Commit("v1.1.0");
        repo.AnnotatedTag("v1.1.0", v110);
        string candidate = repo.Commit("v1.1.1 preparation");
        repo.Branch("release/v1.1", candidate);

        GitReleaseValidationResult result = GitRepositoryValidator.Validate(Request(repo, "1.1.1", candidate));

        await Assert.That(result.IsValid).IsTrue();
        await Assert.That(result.Identity?.ObjectFormat).IsEqualTo("sha256");
        await Assert.That(result.Identity?.OidLength).IsEqualTo(candidate.Length);
        await Assert.That(result.Identity?.CandidateOid).IsEqualTo(candidate);
    }

    [Test]
    public async Task RevisionsAndReleaseBranchesFromAnotherLineFailClosed()
    {
        using var repo = GitRepositoryFixture.Create();
        string v110 = repo.Commit("v1.1.0");
        repo.AnnotatedTag("v1.1.0", v110);
        string candidate = repo.Commit("v1.1.1 preparation");
        repo.Branch("release/v1.2", candidate);

        GitReleaseValidationResult result = GitRepositoryValidator.Validate(new GitReleaseValidationRequest(
            repo.Path,
            "v1.1",
            "1.1.1",
            "refs/heads/release/v1.2",
            "HEAD"));

        await Assert.That(result.Diagnostics).Contains("git_object_id_not_full:candidate");
        await Assert.That(result.Diagnostics).Contains("git_release_branch_line_mismatch");
    }

    [Test]
    public async Task WrongLineAndRecreatedTagObjectsFailClosed()
    {
        using var wrongLine = GitRepositoryFixture.Create();
        string v110 = wrongLine.Commit("v1.1.0");
        wrongLine.AnnotatedTag("v1.1.0", v110);
        wrongLine.Orphan("parallel release line");
        string misplaced = wrongLine.Commit("misplaced v1.1.1");
        wrongLine.AnnotatedTag("v1.1.1", misplaced);
        wrongLine.Checkout("main");
        string candidate = wrongLine.Commit("v1.1.2 preparation");
        wrongLine.Branch("release/v1.1", candidate);

        GitReleaseValidationResult wrongLineResult = GitRepositoryValidator.Validate(Request(wrongLine, "1.1.2", candidate));

        using var recreated = GitRepositoryFixture.Create();
        string stable = recreated.Commit("v1.1.0");
        recreated.AnnotatedTag("v1.1.0", stable);
        string originalTagObject = recreated.Resolve("refs/tags/v1.1.0");
        recreated.DeleteTag("v1.1.0");
        recreated.AnnotatedTag("v1.1.0", stable, "recreated");
        string recreatedCandidate = recreated.Commit("v1.1.1 preparation");
        recreated.Branch("release/v1.1", recreatedCandidate);
        GitReleaseValidationResult recreatedResult = GitRepositoryValidator.Validate(new GitReleaseValidationRequest(
            recreated.Path,
            "v1.1",
            "1.1.1",
            "refs/heads/release/v1.1",
            recreatedCandidate,
            ExpectedTagObjectOids: new Dictionary<string, string> { ["v1.1.0"] = originalTagObject }));

        await Assert.That(wrongLineResult.Diagnostics).Contains("git_wrong_line_tag:v1.1.1");
        await Assert.That(recreatedResult.Diagnostics).Contains("git_tag_object_mismatch:v1.1.0");
    }

    [Test]
    public async Task HostileGitConfigurationIsIgnoredAndTimeoutsAreBounded()
    {
        using var repo = GitRepositoryFixture.Create();
        string v110 = repo.Commit("v1.1.0");
        repo.AnnotatedTag("v1.1.0", v110);
        string candidate = repo.Commit("v1.1.1 preparation");
        repo.Branch("release/v1.1", candidate);
        string hostileConfig = System.IO.Path.Combine(repo.Path, "hostile.gitconfig");
        File.WriteAllText(hostileConfig, "this is not valid git config");
        string? previousGlobal = Environment.GetEnvironmentVariable("GIT_CONFIG_GLOBAL");
        string? previousSystem = Environment.GetEnvironmentVariable("GIT_CONFIG_SYSTEM");
        string? previousCount = Environment.GetEnvironmentVariable("GIT_CONFIG_COUNT");
        string? previousKey = Environment.GetEnvironmentVariable("GIT_CONFIG_KEY_0");
        string? previousValue = Environment.GetEnvironmentVariable("GIT_CONFIG_VALUE_0");
        string? previousGitDirectory = Environment.GetEnvironmentVariable("GIT_DIR");

        try
        {
            Environment.SetEnvironmentVariable("GIT_CONFIG_GLOBAL", hostileConfig);
            Environment.SetEnvironmentVariable("GIT_CONFIG_SYSTEM", hostileConfig);
            Environment.SetEnvironmentVariable("GIT_CONFIG_COUNT", "1");
            Environment.SetEnvironmentVariable("GIT_CONFIG_KEY_0", "invalid key");
            Environment.SetEnvironmentVariable("GIT_CONFIG_VALUE_0", "hostile");
            Environment.SetEnvironmentVariable("GIT_DIR", System.IO.Path.Combine(repo.Path, "not-the-repository"));
            GitReleaseValidationResult isolated = GitRepositoryValidator.Validate(Request(repo, "1.1.1", candidate));
            GitReleaseValidationResult invalidTimeout = GitRepositoryValidator.Validate(Request(repo, "1.1.1", candidate), TimeSpan.FromMinutes(1));

            await Assert.That(isolated.IsValid).IsTrue();
            await Assert.That(invalidTimeout.Diagnostics).Contains("git_timeout_invalid");
        }
        finally
        {
            Environment.SetEnvironmentVariable("GIT_CONFIG_GLOBAL", previousGlobal);
            Environment.SetEnvironmentVariable("GIT_CONFIG_SYSTEM", previousSystem);
            Environment.SetEnvironmentVariable("GIT_CONFIG_COUNT", previousCount);
            Environment.SetEnvironmentVariable("GIT_CONFIG_KEY_0", previousKey);
            Environment.SetEnvironmentVariable("GIT_CONFIG_VALUE_0", previousValue);
            Environment.SetEnvironmentVariable("GIT_DIR", previousGitDirectory);
        }
    }

    [Test]
    public async Task Sha256RepositoriesUseGitReportedObjectLengthWhenSupported()
    {
        using GitRepositoryFixture? repo = GitRepositoryFixture.CreateSha256OrNull();
        if (repo is null)
        {
            return;
        }

        string v110 = repo.Commit("v1.1.0");
        repo.AnnotatedTag("v1.1.0", v110);
        string candidate = repo.Commit("v1.1.1 preparation");
        repo.Branch("release/v1.1", candidate);

        GitReleaseValidationResult result = GitRepositoryValidator.Validate(Request(repo, "1.1.1", candidate));

        await Assert.That(result.IsValid).IsTrue();
        await Assert.That(result.Identity?.ObjectFormat).IsEqualTo("sha256");
        await Assert.That(result.Identity?.OidLength).IsEqualTo(64);
        await Assert.That(result.Identity?.CandidateOid?.Length).IsEqualTo(64);
    }

    private static GitReleaseValidationRequest Request(GitRepositoryFixture repo, string version, string candidate) => new(
        repo.Path,
        "v1.1",
        version,
        "refs/heads/release/v1.1",
        candidate);

    private sealed class GitRepositoryFixture : IDisposable
    {
        private GitRepositoryFixture(string path) => Path = path;

        public string Path { get; }

        public static GitRepositoryFixture Create(string objectFormat = "sha1")
        {
            string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"islamu-git-{Guid.NewGuid():N}");
            Directory.CreateDirectory(path);
            var fixture = new GitRepositoryFixture(path);
            fixture.Git("init", $"--object-format={objectFormat}", "--initial-branch=main");
            fixture.Commit("initial");
            return fixture;
        }

        public static GitRepositoryFixture? CreateSha256OrNull()
        {
            string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"islamu-git-{Guid.NewGuid():N}");
            Directory.CreateDirectory(path);
            var fixture = new GitRepositoryFixture(path);
            if (!TryRun(path, out _, "init", "--object-format=sha256", "--initial-branch=main"))
            {
                fixture.Dispose();
                return null;
            }

            fixture.Commit("initial");
            return fixture;
        }

        public GitRepositoryFixture CloneDepthOne()
        {
            string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"islamu-git-{Guid.NewGuid():N}");
            Run(null, "clone", "--depth", "1", "--branch", "release/v1.1", new Uri(Path).AbsoluteUri, path);
            return new GitRepositoryFixture(path);
        }

        public string Commit(string message)
        {
            File.AppendAllText(System.IO.Path.Combine(Path, "file.txt"), message + Environment.NewLine);
            Git("add", "file.txt");
            Git("-c", "user.name=Release Test", "-c", "user.email=release@example.invalid", "commit", "-m", message);
            return Git("rev-parse", "HEAD").Trim();
        }

        public void AnnotatedTag(string name, string target, string? message = null) => Git("-c", "user.name=Release Test", "-c", "user.email=release@example.invalid", "tag", "-a", name, target, "-m", message ?? name);
        public void LightweightTag(string name, string target) => Git("tag", name, target);
        public void DeleteTag(string name) => Git("tag", "--delete", name);
        public void Branch(string name, string target) => Git("branch", "-f", name, target);
        public void Checkout(string name) => Git("checkout", name);
        public string Resolve(string reference) => Git("rev-parse", "--verify", reference).Trim();
        public void Replace(string oldObject, string newObject) => Git("replace", oldObject, newObject);
        public void MarkPartialClone() => Git("config", "remote.origin.promisor", "true");
        public void WriteGraft(string child, string parent)
        {
            string info = System.IO.Path.Combine(Path, ".git", "info");
            Directory.CreateDirectory(info);
            File.WriteAllText(System.IO.Path.Combine(info, "grafts"), child + " " + parent + Environment.NewLine);
        }

        public void Orphan(string message)
        {
            Git("checkout", "--orphan", "orphan-work");
            File.WriteAllText(System.IO.Path.Combine(Path, "file.txt"), message + Environment.NewLine);
            Git("add", "file.txt");
        }

        private string Git(params string[] args) => Run(Path, args);

        private static string Run(string? workingDirectory, params string[] args)
        {
            if (TryRun(workingDirectory, out string output, args))
            {
                return output;
            }

            throw new InvalidOperationException($"git {string.Join(' ', args)} failed");
        }

        private static bool TryRun(string? workingDirectory, out string output, params string[] args)
        {
            output = string.Empty;
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo("git")
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory,
                },
            };
            foreach (string arg in args)
            {
                process.StartInfo.ArgumentList.Add(arg);
            }
            string nullDevice = OperatingSystem.IsWindows() ? "NUL" : "/dev/null";
            process.StartInfo.Environment["GIT_CONFIG_NOSYSTEM"] = "1";
            process.StartInfo.Environment["GIT_CONFIG_GLOBAL"] = nullDevice;
            process.StartInfo.ArgumentList.Insert(0, $"core.hooksPath={nullDevice}");
            process.StartInfo.ArgumentList.Insert(0, "-c");

            process.Start();
            output = process.StandardOutput.ReadToEnd();
            process.StandardError.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                return false;
            }

            return true;
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
