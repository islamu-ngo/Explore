// ABOUTME: FluentValidation validator for instance governance settings, including runtime render-policy rules.
// ABOUTME: Enforces allowed policy values. Onboarding InteractiveServer guardrail is handled by governance service normalization.

using Explore.Domain.Enums;
using FluentValidation;

namespace Explore.Application.DTOs.Onboarding.Validators;

public class InstanceGovernanceSettingsDtoValidator : AbstractValidator<InstanceGovernanceSettingsDto>
{
    private static readonly string[] AllowedDeploymentModes = ["SingleTenant", "MultiTenant"];

    public InstanceGovernanceSettingsDtoValidator()
    {
        RuleFor(x => x.DeploymentMode)
            .Must(mode => IsInAllowedSet(mode, AllowedDeploymentModes))
            .WithMessage("DeploymentMode must be SingleTenant or MultiTenant.");

        RuleFor(x => x.RenderPolicyVersion)
            .GreaterThan(0)
            .WithMessage("RenderPolicyVersion must be greater than 0.");

        RuleFor(x => x.RenderPolicyPreset)
            .Must(IsValidRenderPolicyPreset)
            .WithMessage("RenderPolicyPreset must be SeoBalanced, AllPrerendered, AllInteractiveAutoNoPrerender, AllInteractiveServer, or CustomAdvanced.");

        RuleFor(x => x.GlobalRenderMode)
            .Must(IsValidRenderMode)
            .WithMessage("GlobalRenderMode must be InteractiveAuto, InteractiveWebAssembly, or InteractiveServer.");

        RuleFor(x => x.PublicSeoRenderMode)
            .Must(IsValidRenderMode)
            .WithMessage("PublicSeoRenderMode must be InteractiveAuto, InteractiveWebAssembly, or InteractiveServer.");

        RuleFor(x => x.OperationalRenderMode)
            .Must(IsValidRenderMode)
            .WithMessage("OperationalRenderMode must be InteractiveAuto, InteractiveWebAssembly, or InteractiveServer.");

        RuleFor(x => x.AdminRenderMode)
            .Must(IsValidRenderMode)
            .WithMessage("AdminRenderMode must be InteractiveAuto, InteractiveWebAssembly, or InteractiveServer.");

        RuleFor(x => x.OnboardingRenderMode)
            .Must(IsValidRenderMode)
            .WithMessage("OnboardingRenderMode must be InteractiveAuto, InteractiveWebAssembly, or InteractiveServer.");

        RuleFor(x => x.EnableAdvancedRenderPolicyOverrides)
            .Equal(true)
            .When(x => string.Equals(x.RenderPolicyPreset, "CustomAdvanced", StringComparison.OrdinalIgnoreCase))
            .WithMessage("EnableAdvancedRenderPolicyOverrides must be true when RenderPolicyPreset is CustomAdvanced.");

        RuleFor(x => x.EnableAdvancedRenderPolicyOverrides)
            .Equal(false)
            .When(x => !string.Equals(x.RenderPolicyPreset, "CustomAdvanced", StringComparison.OrdinalIgnoreCase))
            .WithMessage("EnableAdvancedRenderPolicyOverrides must be false for non-CustomAdvanced presets.");
    }

    private static bool IsInAllowedSet(string? value, string[] allowedValues)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        foreach (var allowed in allowedValues)
        {
            if (allowed.Equals(value, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsValidRenderPolicyPreset(string? preset)
    {
        return Enum.TryParse<RenderPolicyPresetEnum>(preset, ignoreCase: true, out _);
    }

    private static bool IsValidRenderMode(string? mode)
    {
        return TryParseRenderMode(mode, out _);
    }

    private static bool TryParseRenderMode(string? mode, out RenderModeOptionEnum parsed)
    {
        return Enum.TryParse(mode, ignoreCase: true, out parsed);
    }
}
