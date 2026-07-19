// ABOUTME: Component tests for tenant AT Protocol event-federation governance controls.
// ABOUTME: Verifies server-authoritative editability and exact tenant setting writes.

using Explore.Blazor.Client.Contracts.Services.Federation;
using Explore.Blazor.Client.Pages.Admin.Tenant.Components;

namespace Explore.Blazor.Client.Tests.Pages.Admin;

public sealed class TenantPoliciesSectionTests : IDisposable
{
    private readonly BlazorTestContext _ctx = new();
    private readonly IAtprotoFederationSettingsService _settingsService =
        Substitute.For<IAtprotoFederationSettingsService>();

    public TenantPoliciesSectionTests()
    {
        _ctx.Services.AddSingleton(_settingsService);
    }

    public void Dispose() => _ctx.Dispose();

    [Test]
    public async Task AtprotoEvents_WhenEditable_UpdatesCombinedCapabilitySetting()
    {
        _settingsService.GetTenantAsync(Arg.Any<CancellationToken>())
            .Returns(CreateSettings(eventsCanEdit: true));
        _settingsService.UpdateTenantAsync(
                "federation.atproto_events_enabled",
                "true",
                Arg.Any<CancellationToken>())
            .Returns(new BaseCommandResponseOfGuid { Success = true });

        var cut = RenderComponent();
        cut.WaitForState(() => cut.Markup.Contains("Enable AT Protocol events", StringComparison.OrdinalIgnoreCase));

        cut.FindAll("input[type='checkbox']").Last().Change(true);

        await _settingsService.Received(1).UpdateTenantAsync(
            "federation.atproto_events_enabled",
            "true",
            Arg.Any<CancellationToken>());
        await Assert.That(cut.Markup).Contains("both fetching community events and publishing eligible local events", StringComparison.OrdinalIgnoreCase);
    }

    [Test]
    public async Task AtprotoEvents_WhenLocked_DisablesControlAndDoesNotWrite()
    {
        _settingsService.GetTenantAsync(Arg.Any<CancellationToken>())
            .Returns(CreateSettings(
                eventsCanEdit: false,
                reason: "Locked by the instance administrator.",
                source: SettingSource.SystemLocked));

        var cut = RenderComponent();
        cut.WaitForState(() => cut.Markup.Contains("Enable AT Protocol events", StringComparison.OrdinalIgnoreCase));
        var atprotoSwitch = cut.FindAll("input[type='checkbox']").Last();

        await Assert.That(atprotoSwitch.HasAttribute("disabled")).IsTrue();
        await Assert.That(cut.Markup).Contains("Source: System locked", StringComparison.OrdinalIgnoreCase);
        await Assert.That(cut.Markup).Contains("Lock: Locked", StringComparison.OrdinalIgnoreCase);
        await Assert.That(cut.Markup).Contains("Locked by the instance administrator.");
        await Assert.That(cut.FindAll("[role='status']").Any(element =>
            element.TextContent.Contains("Locked by the instance administrator.", StringComparison.Ordinal))).IsTrue();
        await _settingsService.DidNotReceive().UpdateTenantAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AtprotoEvents_WhenLoadFails_RendersSafeAccessibleAlert()
    {
        _settingsService.GetTenantAsync(Arg.Any<CancellationToken>())
            .Returns<Task<SettingGroupResponseDto>>(_ =>
                throw new HttpRequestException("provider credential canary"));

        var cut = RenderComponent();
        cut.WaitForState(() => cut.Markup.Contains(
            "AT Protocol event settings are temporarily unavailable.",
            StringComparison.Ordinal));
        var alert = cut.Find("[role='alert']");

        await Assert.That(alert.TextContent).Contains("temporarily unavailable", StringComparison.OrdinalIgnoreCase);
        await Assert.That(cut.Markup).DoesNotContain("provider credential canary", StringComparison.Ordinal);
        await Assert.That(cut.FindAll("button").Any(button =>
            button.TextContent.Contains("Retry AT Protocol settings", StringComparison.Ordinal))).IsTrue();
    }

    private IRenderedComponent<TenantPoliciesSection> RenderComponent() =>
        _ctx.RenderMudComponent<TenantPoliciesSection>(parameters => parameters
            .Add(component => component.Model, new TenantPolicySettingsDto()));

    private static SettingGroupResponseDto CreateSettings(
        bool eventsCanEdit,
        string? reason = null,
        SettingSource source = SettingSource.TenantOverride) =>
        new()
        {
            Category = "AtprotoFederation",
            Settings =
            [
                new EffectiveSettingDto
                {
                    Key = "federation.atproto_events_enabled",
                    Value = "false",
                    CanEdit = eventsCanEdit,
                    Source = source,
                    IsLocked = source is SettingSource.SystemLocked or SettingSource.TenantLocked,
                    Reason = reason
                },
                new EffectiveSettingDto
                {
                    Key = "federation.atproto_event_validation_profile",
                    Value = "\"platform\"",
                    CanEdit = eventsCanEdit,
                    Source = source,
                    IsLocked = source is SettingSource.SystemLocked or SettingSource.TenantLocked,
                    Reason = reason
                }
            ]
        };
}
