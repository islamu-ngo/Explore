// ABOUTME: Unit tests for InstanceGovernanceSettingsDtoValidator render-policy governance rules.
// ABOUTME: Verifies allowed policy values and onboarding InteractiveServer guardrails.

using System.Linq;
using Explore.Application.DTOs.Onboarding;
using Explore.Application.DTOs.Onboarding.Validators;

namespace Event.Application.UnitTests.DTOs.Onboarding.Validators;

public class InstanceGovernanceSettingsDtoValidatorTests
{
    [Test]
    public async Task ValidateAsync_WithValidSettings_ReturnsValid()
    {
        var validator = new InstanceGovernanceSettingsDtoValidator();
        var dto = CreateValidDto();

        var result = await validator.ValidateAsync(dto);

        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task ValidateAsync_WithInteractiveServerOnOnboarding_ReturnsInvalid()
    {
        var validator = new InstanceGovernanceSettingsDtoValidator();
        var dto = CreateValidDto();
        dto.OnboardingRenderMode = "InteractiveServer";
        dto.DisallowInteractiveServerOnOnboarding = false;

        var result = await validator.ValidateAsync(dto);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Select(x => x.ErrorMessage)).Contains("OnboardingRenderMode cannot be InteractiveServer.");
        await Assert.That(result.Errors.Select(x => x.ErrorMessage)).Contains("DisallowInteractiveServerOnOnboarding must remain enabled.");
    }

    [Test]
    public async Task ValidateAsync_WithCustomAdvancedPresetAndAdvancedOverridesDisabled_ReturnsInvalid()
    {
        var validator = new InstanceGovernanceSettingsDtoValidator();
        var dto = CreateValidDto();
        dto.RenderPolicyPreset = "CustomAdvanced";
        dto.EnableAdvancedRenderPolicyOverrides = false;

        var result = await validator.ValidateAsync(dto);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Select(x => x.ErrorMessage))
            .Contains("EnableAdvancedRenderPolicyOverrides must be true when RenderPolicyPreset is CustomAdvanced.");
    }

    private static InstanceGovernanceSettingsDto CreateValidDto()
    {
        return new InstanceGovernanceSettingsDto
        {
            DeploymentMode = "SingleTenant",
            RenderPolicyVersion = 1,
            RenderPolicyPreset = "SeoBalanced",
            EnableAdvancedRenderPolicyOverrides = false,
            PublicSeoRenderMode = "InteractiveAuto",
            PublicSeoPrerenderEnabled = true,
            OperationalRenderMode = "InteractiveAuto",
            OperationalPrerenderEnabled = false,
            AdminRenderMode = "InteractiveAuto",
            AdminPrerenderEnabled = false,
            OnboardingRenderMode = "InteractiveAuto",
            OnboardingPrerenderEnabled = false,
            DisallowInteractiveServerOnOnboarding = true
        };
    }
}
