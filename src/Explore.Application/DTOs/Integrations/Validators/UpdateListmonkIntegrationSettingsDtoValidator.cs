// ABOUTME: Validates Listmonk integration non-secret settings before tenant-scope persistence.
// ABOUTME: Enforces minimal SSRF guardrails for configured instance URLs without network lookups.

namespace Explore.Application.DTOs.Integrations.Validators;

using System.Net;
using FluentValidation;

public sealed class UpdateListmonkIntegrationSettingsDtoValidator
    : AbstractValidator<UpdateListmonkIntegrationSettingsDto>
{
    public UpdateListmonkIntegrationSettingsDtoValidator()
    {
        RuleFor(x => x.DefaultListId)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Listmonk default list ID cannot be negative.");

        RuleFor(x => x.DefaultListId)
            .GreaterThan(0)
            .When(x => x.Enabled || x.SyncOnRegistration)
            .WithMessage("Listmonk default list ID is required when the integration is enabled.");

        RuleFor(x => x.InstanceUrl)
            .NotEmpty()
            .When(x => x.Enabled || x.SyncOnRegistration)
            .WithMessage("Listmonk instance URL is required when the integration is enabled.");

        RuleFor(x => x.InstanceUrl)
            .Must(BeSafeHttpUrl)
            .When(x => !string.IsNullOrWhiteSpace(x.InstanceUrl))
            .WithMessage("Listmonk instance URL must be an absolute public HTTP(S) URL.");

        RuleFor(x => x.SyncOnRegistration)
            .Must((dto, syncOnRegistration) => !syncOnRegistration || dto.Enabled)
            .WithMessage("Listmonk registration sync requires the integration to be enabled.");
    }

    private static bool BeSafeHttpUrl(string? value)
    {
        if (!Uri.TryCreate(value?.Trim(), UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
            string.IsNullOrWhiteSpace(uri.Host))
        {
            return false;
        }

        if (uri.IsLoopback ||
            string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase) ||
            uri.Host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return !IPAddress.TryParse(uri.Host, out var address) || IsPublicAddress(address);
    }

    private static bool IsPublicAddress(IPAddress address)
    {
        if (IPAddress.IsLoopback(address))
            return false;

        if (address.Equals(IPAddress.Any) ||
            address.Equals(IPAddress.IPv6Any) ||
            address.Equals(IPAddress.Broadcast) ||
            address.Equals(IPAddress.None))
        {
            return false;
        }

        if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            return !address.IsIPv6LinkLocal && !address.IsIPv6SiteLocal;
        }

        var bytes = address.GetAddressBytes();
        return bytes[0] switch
        {
            10 => false,
            127 => false,
            169 when bytes[1] == 254 => false,
            172 when bytes[1] >= 16 && bytes[1] <= 31 => false,
            192 when bytes[1] == 168 => false,
            _ => true
        };
    }
}
