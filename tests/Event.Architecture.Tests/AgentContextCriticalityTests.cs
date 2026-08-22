// ABOUTME: Architecture assertions enforcing the 5-Tier Work Criticality Taxonomy, model tiers, and safety gates.
// ABOUTME: Verifies that every intent in intents.yaml adheres to risk-to-rigor rules and mandatory review protocols.

using System.Text.Json;
using System.Text.RegularExpressions;

namespace Event.Architecture.Tests;

public class AgentContextCriticalityTests
{
    private static readonly string[] AllowedTiers = ["sovereign", "security", "privacy", "domain_state", "standard"];
    private static readonly string[] AllowedModelTiers = ["economical", "balanced", "advanced"];
    private static readonly string[] AllowedVerificationDepths = ["standard", "extended", "exhaustive"];
    private static readonly string[] AllowedTenancyScopes = ["tenant_isolated", "cross_tenant_admin", "system_level"];
    private static readonly string[] AllowedRollbackStrategies = ["expand_contract", "atomic_revert", "forward_fix_only"];
    private static readonly string[] AllowedIntakeClarificationModes = ["mandatory_grill_me", "standard_qa", "autonomous_defaults"];
    private static readonly string[] AllowedExplorationProtocols = ["exhaustive_graph_blast_radius", "bounded_caller_callee", "local_surface_only"];
    private static readonly string[] AllowedTestingStrategies = ["adversarial_concurrency_invariant_breakers", "behavioral_unit_integration", "affordance_render_only"];
    private static readonly string[] AllowedReviewProtocols = ["anonymized_epistemic_mad", "peer_review", "lightweight_self_check"];

    [Test]
    [DisplayName("Every intent in intents.yaml must declare a valid criticality configuration")]
    public async Task Intents_MustDeclareValidCriticality()
    {
        var root = ContextSystemHelpers.RepoRoot;
        var intentsPath = Path.Combine(root, ".agents", "contract", "intents.yaml");
        var text = File.ReadAllText(intentsPath);

        var intentBlocks = Regex.Split(text, @"\n\s*-\s+id:\s+")
            .Where(b => !string.IsNullOrWhiteSpace(b) && !b.StartsWith("#"))
            .ToArray();

        var violations = new List<string>();

        foreach (var block in intentBlocks)
        {
            var lines = block.Split('\n');
            var id = lines[0].Trim().Trim('"');

            var tierMatch = Regex.Match(block, @"\n\s*tier:\s*([a-z_]+)");
            var modelTierMatch = Regex.Match(block, @"\n\s*required_model_tier:\s*([a-z_]+)");
            var depthMatch = Regex.Match(block, @"\n\s*verification_depth:\s*([a-z_]+)");
            var tenancyMatch = Regex.Match(block, @"\n\s*tenancy_scope:\s*([a-z_]+)");
            var rollbackMatch = Regex.Match(block, @"\n\s*rollback_strategy:\s*([a-z_]+)");
            var intakeMatch = Regex.Match(block, @"\n\s*intake_clarification_mode:\s*([a-z_]+)");
            var explorationMatch = Regex.Match(block, @"\n\s*exploration_protocol:\s*([a-z_]+)");
            var testStrategyMatch = Regex.Match(block, @"\n\s*testing_strategy:\s*([a-z_]+)");
            var reviewProtocolMatch = Regex.Match(block, @"\n\s*review_protocol:\s*([a-z_]+)");

            if (!tierMatch.Success || !AllowedTiers.Contains(tierMatch.Groups[1].Value))
            {
                violations.Add($"{id}: missing or invalid criticality.tier ({tierMatch.Groups[1].Value})");
            }

            if (!modelTierMatch.Success || !AllowedModelTiers.Contains(modelTierMatch.Groups[1].Value))
            {
                violations.Add($"{id}: missing or invalid criticality.required_model_tier ({modelTierMatch.Groups[1].Value})");
            }

            if (!depthMatch.Success || !AllowedVerificationDepths.Contains(depthMatch.Groups[1].Value))
            {
                violations.Add($"{id}: missing or invalid criticality.verification_depth ({depthMatch.Groups[1].Value})");
            }

            if (!tenancyMatch.Success || !AllowedTenancyScopes.Contains(tenancyMatch.Groups[1].Value))
            {
                violations.Add($"{id}: missing or invalid criticality.tenancy_scope ({tenancyMatch.Groups[1].Value})");
            }

            if (!rollbackMatch.Success || !AllowedRollbackStrategies.Contains(rollbackMatch.Groups[1].Value))
            {
                violations.Add($"{id}: missing or invalid criticality.rollback_strategy ({rollbackMatch.Groups[1].Value})");
            }

            if (!intakeMatch.Success || !AllowedIntakeClarificationModes.Contains(intakeMatch.Groups[1].Value))
            {
                violations.Add($"{id}: missing or invalid criticality.intake_clarification_mode ({intakeMatch.Groups[1].Value})");
            }

            if (!explorationMatch.Success || !AllowedExplorationProtocols.Contains(explorationMatch.Groups[1].Value))
            {
                violations.Add($"{id}: missing or invalid criticality.exploration_protocol ({explorationMatch.Groups[1].Value})");
            }

            if (!testStrategyMatch.Success || !AllowedTestingStrategies.Contains(testStrategyMatch.Groups[1].Value))
            {
                violations.Add($"{id}: missing or invalid criticality.testing_strategy ({testStrategyMatch.Groups[1].Value})");
            }

            if (!reviewProtocolMatch.Success || !AllowedReviewProtocols.Contains(reviewProtocolMatch.Groups[1].Value))
            {
                violations.Add($"{id}: missing or invalid criticality.review_protocol ({reviewProtocolMatch.Groups[1].Value})");
            }

            if (!block.Contains("safety_gates:"))
            {
                violations.Add($"{id}: missing criticality.safety_gates");
            }
        }

        await Assert.That(violations).IsEmpty()
            .Because("every intent in the Contribution Contract must declare a valid 5-tier criticality configuration");
    }

    [Test]
    [DisplayName("Tier 0, 1, and 2 intents must strictly mandate proactive grill-me, blast-radius exploration, invariant-breakers, and Epistemic MAD")]
    public async Task HighCriticalityIntents_MustMandateDynamicExecutionRigor()
    {
        var root = ContextSystemHelpers.RepoRoot;
        var intentsPath = Path.Combine(root, ".agents", "contract", "intents.yaml");
        var text = File.ReadAllText(intentsPath);

        var intentBlocks = Regex.Split(text, @"\n\s*-\s+id:\s+")
            .Where(b => !string.IsNullOrWhiteSpace(b) && !b.StartsWith("#"))
            .ToArray();

        var violations = new List<string>();

        foreach (var block in intentBlocks)
        {
            var lines = block.Split('\n');
            var id = lines[0].Trim().Trim('"');

            var tier = Regex.Match(block, @"\n\s*tier:\s*([a-z_]+)").Groups[1].Value;
            var intake = Regex.Match(block, @"\n\s*intake_clarification_mode:\s*([a-z_]+)").Groups[1].Value;
            var exploration = Regex.Match(block, @"\n\s*exploration_protocol:\s*([a-z_]+)").Groups[1].Value;
            var testStrategy = Regex.Match(block, @"\n\s*testing_strategy:\s*([a-z_]+)").Groups[1].Value;
            var review = Regex.Match(block, @"\n\s*review_protocol:\s*([a-z_]+)").Groups[1].Value;

            if (tier is "sovereign" or "security" or "privacy")
            {
                if (intake != "mandatory_grill_me")
                {
                    violations.Add($"{id}: high-criticality tier '{tier}' must mandate intake_clarification_mode: mandatory_grill_me (found '{intake}')");
                }

                if (exploration != "exhaustive_graph_blast_radius")
                {
                    violations.Add($"{id}: high-criticality tier '{tier}' must mandate exploration_protocol: exhaustive_graph_blast_radius (found '{exploration}')");
                }

                if (testStrategy != "adversarial_concurrency_invariant_breakers")
                {
                    violations.Add($"{id}: high-criticality tier '{tier}' must mandate testing_strategy: adversarial_concurrency_invariant_breakers (found '{testStrategy}')");
                }

                if (review != "anonymized_epistemic_mad")
                {
                    violations.Add($"{id}: high-criticality tier '{tier}' must mandate review_protocol: anonymized_epistemic_mad (found '{review}')");
                }
            }
        }

        await Assert.That(violations).IsEmpty()
            .Because("high-criticality intents must strictly enforce dynamic intake, exploration, invariant-breaker, and multi-agent review rigor");
    }

    [Test]
    [DisplayName("Tier 0, 1, and 2 intents must strictly require advanced model tier and exhaustive verification")]
    public async Task HighCriticalityIntents_MustRequireAdvancedTierAndExhaustiveDepth()
    {
        var root = ContextSystemHelpers.RepoRoot;
        var intentsPath = Path.Combine(root, ".agents", "contract", "intents.yaml");
        var text = File.ReadAllText(intentsPath);

        var intentBlocks = Regex.Split(text, @"\n\s*-\s+id:\s+")
            .Where(b => !string.IsNullOrWhiteSpace(b) && !b.StartsWith("#"))
            .ToArray();

        var violations = new List<string>();

        foreach (var block in intentBlocks)
        {
            var lines = block.Split('\n');
            var id = lines[0].Trim().Trim('"');

            var tier = Regex.Match(block, @"\n\s*tier:\s*([a-z_]+)").Groups[1].Value;
            var modelTier = Regex.Match(block, @"\n\s*required_model_tier:\s*([a-z_]+)").Groups[1].Value;
            var depth = Regex.Match(block, @"\n\s*verification_depth:\s*([a-z_]+)").Groups[1].Value;

            if (tier is "sovereign" or "security" or "privacy")
            {
                if (modelTier != "advanced")
                {
                    violations.Add($"{id}: tier '{tier}' must mandate required_model_tier: advanced (found '{modelTier}')");
                }

                if (depth != "exhaustive")
                {
                    violations.Add($"{id}: tier '{tier}' must mandate verification_depth: exhaustive (found '{depth}')");
                }
            }
        }

        await Assert.That(violations).IsEmpty()
            .Because("high-criticality intents (sovereign, security, privacy) require advanced model reasoning and exhaustive verification");
    }

    [Test]
    [DisplayName("Sovereign and Privacy intents must enforce outbox usage in safety gates")]
    public async Task SovereignAndPrivacyIntents_MustEnforceOutboxUsage()
    {
        var root = ContextSystemHelpers.RepoRoot;
        var intentsPath = Path.Combine(root, ".agents", "contract", "intents.yaml");
        var text = File.ReadAllText(intentsPath);

        var intentBlocks = Regex.Split(text, @"\n\s*-\s+id:\s+")
            .Where(b => !string.IsNullOrWhiteSpace(b) && !b.StartsWith("#"))
            .ToArray();

        var violations = new List<string>();

        foreach (var block in intentBlocks)
        {
            var lines = block.Split('\n');
            var id = lines[0].Trim().Trim('"');

            var tier = Regex.Match(block, @"\n\s*tier:\s*([a-z_]+)").Groups[1].Value;

            if (tier is "sovereign" or "privacy")
            {
                if (!block.Contains("outbox", StringComparison.OrdinalIgnoreCase))
                {
                    violations.Add($"{id}: tier '{tier}' must mandate a transactional outbox safety gate");
                }
            }
        }

        await Assert.That(violations).IsEmpty()
            .Because("financial and privacy state changes must decouple side effects through transactional outbox");
    }

    [Test]
    [DisplayName("Sovereign and persistence migration intents must require expand_contract rollback strategy")]
    public async Task SovereignAndMigrationIntents_MustRequireExpandContractRollback()
    {
        var root = ContextSystemHelpers.RepoRoot;
        var intentsPath = Path.Combine(root, ".agents", "contract", "intents.yaml");
        var text = File.ReadAllText(intentsPath);

        var intentBlocks = Regex.Split(text, @"\n\s*-\s+id:\s+")
            .Where(b => !string.IsNullOrWhiteSpace(b) && !b.StartsWith("#"))
            .ToArray();

        var violations = new List<string>();

        foreach (var block in intentBlocks)
        {
            var lines = block.Split('\n');
            var id = lines[0].Trim().Trim('"');

            var tier = Regex.Match(block, @"\n\s*tier:\s*([a-z_]+)").Groups[1].Value;
            var rollback = Regex.Match(block, @"\n\s*rollback_strategy:\s*([a-z_]+)").Groups[1].Value;

            if (id.Contains("migration") || tier == "sovereign")
            {
                if (rollback != "expand_contract")
                {
                    violations.Add($"{id}: must mandate rollback_strategy: expand_contract for zero-downtime evolution (found '{rollback}')");
                }
            }
        }

        await Assert.That(violations).IsEmpty()
            .Because("persistence migrations and sovereign financial workflows must guarantee zero-downtime rolling update safety");
    }
}
