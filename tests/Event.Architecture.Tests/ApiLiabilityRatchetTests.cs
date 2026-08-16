// ABOUTME: Forward-only ratchets for the API liability classes being removed by the liability-reduction workstream.
// ABOUTME: Each baseline is an exact allowlist, so a new violation fails and a fixed violation must be delisted.

namespace Event.Architecture.Tests;

using System.Text.RegularExpressions;

/// <summary>
/// These guardrails exist because the API grew faster than it was being cleaned: between 2026-08-13 and
/// 2026-08-15 the controller surface gained 444 lines while a consolidation phase was removing 174.
/// Enforcement therefore precedes migration instead of following it.
/// <para>
/// Every baseline below is an <em>exact</em> set, not a ceiling. Introducing a new occurrence fails the
/// ratchet, and removing an occurrence without delisting it also fails, which is what forces each list to
/// shrink monotonically toward empty. Entries carry the reason they still exist and the phase that removes them.
/// </para>
/// <para>
/// Deliberately absent: line-count percentages, constructor-syntax rules, and file-count limits. Those measure
/// style rather than liability, and the workstream rejects them as acceptance criteria.
/// </para>
/// </summary>
public class ApiLiabilityRatchetTests
{
    private static readonly string ControllersRoot = ContextSystemHelpers.RepoPath("Explore.API", "Controllers");
    private static readonly string BackgroundServicesRoot = ContextSystemHelpers.RepoPath("Explore.API", "BackgroundServices");

    private static readonly Regex ControllerServiceLocationRegex = new(
        @"RequestServices",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex ControllerClaimParsingRegex = new(
        @"(FindFirst\s*\(|User\s*\.\s*Find)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex PrivateFailureMapperRegex = new(
        @"private\s+[A-Za-z0-9_<>,\[\]\?\. ]+\s+(Map[A-Za-z0-9_]*Failure|To[A-Za-z0-9_]*Problem)\s*\(",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex TimerLoopRegex = new(
        @"Task\s*\.\s*Delay\s*\(",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// Ratchet A — controllers must receive dependencies through the constructor, never the container.
    /// Cleared in Phase 2.2: identity now projects from <c>ControllerBase.User</c>, so no controller resolves
    /// a service to learn who the caller is. The empty baseline makes any reintroduction a failure.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> ControllerServiceLocationBaseline =
        new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>
    /// Ratchet B — ordinary controllers must not reconstruct identity from raw claims. Every remaining entry
    /// is a purpose-bound or diagnostic read that deliberately stays outside the ambient identity authority.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> ControllerClaimParsingBaseline =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["AtprotoSessionController.cs"] = "Purpose-bound ATProto protocol claim validation at the authentication boundary; intentionally retained, not ordinary user context.",
            ["PrivacyErasureController.cs"] = "Purpose-bound erasure-receipt intent claim; intentionally isolated and must never become ambient identity.",
            ["ManagementController.cs"] = "Purpose-bound managed-control-plane instance claim; protocol validation, not user identity.",
            ["AdminCacheDiagnosticsController.cs"] = "Display-safe diagnostic claim slots behind a Development/Testing gate; intentionally retained.",
            ["InstanceOnboardingController.cs"] = "Logs the raw identity claim slots on the onboarding rejection path so operators can diagnose why resolution failed; the identity itself comes from the canonical authority.",
        };

    /// <summary>
    /// Ratchet C — command failures route through a declared <c>CommandFailurePolicy</c>, not a per-action
    /// switch. Phase 3 converged ten of the eleven original mappers; the survivor builds a validation problem
    /// with handler-supplied error lists, which is genuinely feature-specific rather than table-shaped.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> PrivateFailureMapperBaseline =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["WebhooksController.cs"] = "ToWebhookPortalProblem composes provider error lists and a custom detail/code pair, which the declarative policy deliberately does not model.",
        };

    /// <summary>
    /// Ratchet D — periodic work belongs to the Quartz.NET scheduler, not to hand-rolled timer loops.
    /// Queue-driven, drain-runner, and startup-gate services are not periodic and are never listed here.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> TimerLoopBaseline =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["OutboxProcessor.cs"] = "Durable side-effect authority; excluded from scheduler migration so outbox fencing and retry stay untouched.",
            ["ManagedControlPlaneRegistrationWorker.cs"] = "Retry-until-registered bootstrap, not a periodic sweep: it returns once the control plane acknowledges, so a recurring trigger would change its meaning.",
            ["CerbosPolicyBootSyncRunner.cs"] = "Boot-time policy sync with backoff; evaluated in Phase 5.2.",
            ["CerbosPolicyBootSyncWorker.cs"] = "Boot-time policy sync wrapper; evaluated in Phase 5.2.",
            ["EmailDispatchProcessor.cs"] = "Legacy in-process dispatch loop retained while Quartz owns dispatch drain; removed in Phase 5.3.",
            ["IntegrationSyncProcessor.cs"] = "Periodic integration outbox drain; migrates to a Quartz cron job in Phase 5.3.",
            ["PdsSyncWorker.cs"] = "Periodic PDS outbox drain; migrates to a Quartz cron job in Phase 5.3.",
            ["WebhookBulkReplayProcessor.cs"] = "Periodic bulk replay drain; migrates to a Quartz cron job in Phase 5.3.",
            ["WebhookDeliveryProcessor.cs"] = "Periodic webhook delivery drain; migrates to a Quartz cron job in Phase 5.3.",
        };

    /// <summary>
    /// Ratchet E — controller size. Phase 7 partitioned all five original hotspots by route capability, so the
    /// exceptions that remain are two secondary controllers that were never part of that scope.
    /// <para>
    /// The partition preserved every route template and <c>Name = RouteNames.*</c>, which is why it moved 756
    /// OpenAPI operations and 756 generated client methods without changing one of them.
    /// </para>
    /// </summary>
    private static readonly IReadOnlyDictionary<string, int> ControllerLineCeilingBaseline =
        new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["GuestRegistrationOrderController.cs"] = 514,
            ["EventSessionController.cs"] = 546,
            ["AiAssistantController.cs"] = 532,
        };

    /// <summary>Any controller not listed in <see cref="ControllerLineCeilingBaseline"/> must stay under this size.</summary>
    private const int NewControllerLineCeiling = 500;

    /// <summary>Ratchet F — HAL registration boilerplate may not grow; Phase 4.2 replaces triples with helpers.</summary>
    private const int HateoasAssemblerRegistrationCallCeiling = 27;

    [Test]
    public async Task Ratchet_ControllersMustNotResolveServicesFromTheContainer()
    {
        var offenders = ScanFiles(ControllersRoot, ControllerServiceLocationRegex);
        await AssertRatchet(offenders, ControllerServiceLocationBaseline, "controller service location");
    }

    [Test]
    public async Task Ratchet_ControllersMustNotReconstructIdentityFromClaims()
    {
        var offenders = ScanFiles(ControllersRoot, ControllerClaimParsingRegex);
        await AssertRatchet(offenders, ControllerClaimParsingBaseline, "controller identity claim parsing");
    }

    [Test]
    public async Task Ratchet_ControllersMustNotOwnPrivateCommandFailureMappers()
    {
        var offenders = ScanFiles(ControllersRoot, PrivateFailureMapperRegex);
        await AssertRatchet(offenders, PrivateFailureMapperBaseline, "private controller failure mapping");
    }

    [Test]
    public async Task Ratchet_BackgroundServicesMustNotHandRollTimerLoops()
    {
        var offenders = ScanFiles(BackgroundServicesRoot, TimerLoopRegex);
        await AssertRatchet(offenders, TimerLoopBaseline, "hand-rolled periodic timer loop");
    }

    [Test]
    public async Task Ratchet_ControllersMustNotGrowBeyondTheirRecordedCeiling()
    {
        var failures = new List<string>();

        foreach (var file in EnumerateSourceFiles(ControllersRoot))
        {
            var name = Path.GetFileName(file);
            var lineCount = File.ReadAllLines(file).Length;
            var ceiling = ControllerLineCeilingBaseline.TryGetValue(name, out var recorded)
                ? recorded
                : NewControllerLineCeiling;

            if (lineCount > ceiling)
            {
                failures.Add(ControllerLineCeilingBaseline.ContainsKey(name)
                    ? $"{name} grew to {lineCount} lines, above its recorded ceiling of {ceiling}. Hotspot controllers may only shrink."
                    : $"{name} is {lineCount} lines, above the {NewControllerLineCeiling}-line limit for controllers outside the recorded hotspot set.");
            }
        }

        Report("controller size ceiling", failures);
        await Assert.That(failures).IsEmpty();
    }

    [Test]
    public async Task Ratchet_HateoasRegistrationBoilerplateMustNotGrow()
    {
        var registrationFile = ContextSystemHelpers.RepoPath("Explore.API", "Extensions", "HateoasAssemblerRegistration.cs");
        var source = await File.ReadAllTextAsync(registrationFile);
        var registrationCount = Regex.Matches(source, @"AddScoped", RegexOptions.CultureInvariant).Count;

        var failures = registrationCount > HateoasAssemblerRegistrationCallCeiling
            ? new List<string>
            {
                $"HateoasAssemblerRegistration.cs now performs {registrationCount} AddScoped registrations, above the recorded ceiling of " +
                $"{HateoasAssemblerRegistrationCallCeiling}. Register new HAL resources through the compile-time helpers instead of adding raw triples.",
            }
            : [];

        Report("HAL registration boilerplate", failures);
        await Assert.That(failures).IsEmpty();
    }

    /// <summary>
    /// The ceiling baseline is only meaningful while the files it names exist, so a rename or deletion that
    /// leaves a stale entry behind is itself a failure.
    /// </summary>
    [Test]
    public async Task RatchetBaselines_MustReferenceExistingFilesAndCarryReasons()
    {
        var controllerFiles = EnumerateSourceFiles(ControllersRoot).Select(Path.GetFileName).ToHashSet(StringComparer.Ordinal);
        var backgroundFiles = EnumerateSourceFiles(BackgroundServicesRoot).Select(Path.GetFileName).ToHashSet(StringComparer.Ordinal);

        var failures = new List<string>();
        failures.AddRange(ValidateBaselineTargets(ControllerServiceLocationBaseline, controllerFiles, "controller service location"));
        failures.AddRange(ValidateBaselineTargets(ControllerClaimParsingBaseline, controllerFiles, "controller identity claim parsing"));
        failures.AddRange(ValidateBaselineTargets(PrivateFailureMapperBaseline, controllerFiles, "private controller failure mapping"));
        failures.AddRange(ValidateBaselineTargets(TimerLoopBaseline, backgroundFiles, "hand-rolled periodic timer loop"));

        failures.AddRange(ControllerLineCeilingBaseline
            .Where(entry => !controllerFiles.Contains(entry.Key))
            .Select(entry => $"controller size ceiling baseline names '{entry.Key}', which no longer exists. Delete the entry."));

        Report("ratchet baseline hygiene", failures);
        await Assert.That(failures).IsEmpty();
    }

    private static IEnumerable<string> ValidateBaselineTargets(
        IReadOnlyDictionary<string, string> baseline,
        IReadOnlyCollection<string> existingFiles,
        string ratchetName) => baseline
        .Where(entry => !existingFiles.Contains(entry.Key) || string.IsNullOrWhiteSpace(entry.Value))
        .Select(entry => string.IsNullOrWhiteSpace(entry.Value)
            ? $"{ratchetName} baseline entry '{entry.Key}' has no reason recorded."
            : $"{ratchetName} baseline names '{entry.Key}', which no longer exists. Delete the entry.");

    private static IReadOnlyCollection<string> ScanFiles(string root, Regex pattern) => EnumerateSourceFiles(root)
        .Where(file => pattern.IsMatch(File.ReadAllText(file)))
        .Select(Path.GetFileName)
        .OfType<string>()
        .ToHashSet(StringComparer.Ordinal);

    private static IEnumerable<string> EnumerateSourceFiles(string root) => Directory.Exists(root)
        ? Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
        : [];

    /// <summary>
    /// Fails in both directions: an unlisted offender is a regression, and a listed file that no longer
    /// offends means the ratchet advanced and its entry must be deleted so the baseline cannot silently refill.
    /// </summary>
    private static async Task AssertRatchet(
        IReadOnlyCollection<string> offenders,
        IReadOnlyDictionary<string, string> baseline,
        string ratchetName)
    {
        var failures = new List<string>();

        failures.AddRange(offenders
            .Where(offender => !baseline.ContainsKey(offender))
            .Order(StringComparer.Ordinal)
            .Select(offender => $"{offender} introduces {ratchetName}, which this workstream is removing. " +
                "Use the current authority instead of adding a new occurrence."));

        failures.AddRange(baseline.Keys
            .Where(listed => !offenders.Contains(listed))
            .Order(StringComparer.Ordinal)
            .Select(listed => $"{listed} no longer contains {ratchetName}. Delete its baseline entry so the ratchet cannot regress."));

        Report(ratchetName, failures);
        await Assert.That(failures).IsEmpty();
    }

    private static void Report(string ratchetName, IReadOnlyCollection<string> failures)
    {
        if (failures.Count == 0)
        {
            return;
        }

        Console.WriteLine($"Ratchet '{ratchetName}' failures ({failures.Count}):");
        foreach (var failure in failures)
        {
            Console.WriteLine($"  - {failure}");
        }
    }
}
