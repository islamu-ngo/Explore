// ABOUTME: Enforces machine-readable I-VSD invocation, report, and integration contracts.
// ABOUTME: Preserves standalone routing while preventing stale planning and CTO handoffs.

namespace Event.Architecture.Tests;

public class IvsdAgentContextContractTests
{
    [Test]
    [DisplayName("I-VSD contracts preserve standalone routing and integrated freshness")]
    public async Task IvsdContracts_ShouldPreserveStandaloneRoutingAndIntegratedFreshness()
    {
        var root = FindRepoRoot();
        var requirements = new Dictionary<string, string[]>
        {
            [".agents/skills/i-vsd/resources/integration-contract.md"] =
            [
                "integration_contract_version: 1",
                "modes: [standalone, planning, plan-review]",
                "standalone_no_context: action-menu",
                "standalone_alignment: grill-me",
                "routing_response_persistence: none",
                "substantive_output_persistence: markdown",
                "planning_request_satisfies_context_agreement: true",
                "cto_review_grants_user_approval: false",
                "material_rewrite_invalidates_i_vsd: true"
            ],
            [".agents/skills/i-vsd/resources/report-contract.md"] =
            [
                "report_contract_version: 1",
                "identity: subject-and-report-kind",
                "finding_ids: stable",
                "report_states: [draft, current, stale, superseded, closed]",
                "dispositions: [advisory, ready-for-planning, plan-aligned, changes-required, escalation-required]",
                "last_updated_required: true"
            ],
            [".agents/skills/i-vsd/SKILL.md"] =
            [
                "resources/integration-contract.md",
                "resources/report-contract.md",
                "../grill-me/SKILL.md"
            ],
            [".agents/skills/implementation-plan/SKILL.md"] =
            [
                "../i-vsd/resources/integration-contract.md"
            ],
            [".agents/skills/senior-cto-feedback/SKILL.md"] =
            [
                "../i-vsd/resources/integration-contract.md"
            ]
        };

        var violations = new List<string>();

        foreach (var (relativePath, requiredTokens) in requirements)
        {
            var path = Path.Combine(root, relativePath);
            if (!File.Exists(path))
            {
                violations.Add($"{relativePath}: missing file");
                continue;
            }

            var contents = File.ReadAllText(path);
            violations.AddRange(requiredTokens
                .Where(token => !contents.Contains(token, StringComparison.Ordinal))
                .Select(token => $"{relativePath}: missing {token}"));
        }

        await Assert.That(violations).IsEmpty()
            .Because("I-VSD must preserve standalone action/context routing and revision-bound integrated handoffs");
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Explore.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root containing Explore.slnx.");
    }
}
