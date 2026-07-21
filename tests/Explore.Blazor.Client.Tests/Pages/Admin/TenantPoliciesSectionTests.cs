// ABOUTME: Component tests for tenant AT Protocol event-federation governance controls.
// ABOUTME: Verifies server-authoritative editability and exact tenant setting writes.

using Explore.Blazor.Client.Contracts.Services.Federation;
using Explore.Blazor.Client.Pages.Admin.Tenant.Components;
using MudBlazor;

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

        var eventsLabel = cut.FindAll("label").Single(label =>
            label.TextContent.Contains("Enable AT Protocol events", StringComparison.OrdinalIgnoreCase));
        (eventsLabel.QuerySelector("input") ?? throw new InvalidOperationException("AT Protocol events switch input not found."))
            .Change(true);

        await _settingsService.Received(1).UpdateTenantAsync(
            "federation.atproto_events_enabled",
            "true",
            Arg.Any<CancellationToken>());
        var capabilityCopy = cut.FindAll("p")
            .Single(element => element.TextContent.Contains("public inbound discovery", StringComparison.OrdinalIgnoreCase))
            .TextContent;
        await Assert.That(capabilityCopy).Contains("public inbound discovery works without AT\u00A0Protocol authentication", StringComparison.OrdinalIgnoreCase);
        await Assert.That(capabilityCopy).Contains("Publishing and RSVP synchronization require AT\u00A0Protocol authentication", StringComparison.OrdinalIgnoreCase);
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
        var eventsLabel = cut.FindAll("label").Single(label =>
            label.TextContent.Contains("Enable AT Protocol events", StringComparison.OrdinalIgnoreCase));
        var atprotoSwitch = eventsLabel.QuerySelector("input")
            ?? throw new InvalidOperationException("AT Protocol events switch input not found.");

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

    [Test]
    public async Task AtprotoEvents_WhenApplicationSessionExpired_RequestsApplicationSignIn()
    {
        _settingsService.GetTenantAsync(Arg.Any<CancellationToken>())
            .Returns<Task<SettingGroupResponseDto>>(_ =>
                throw new ApiException(
                    "Unauthorized",
                    401,
                    response: null,
                    new Dictionary<string, IEnumerable<string>>(),
                    innerException: null));

        var cut = RenderComponent();
        cut.WaitForState(() => cut.Markup.Contains(
            "Your application session expired.",
            StringComparison.Ordinal));
        var alert = cut.Find("[role='alert']");

        await Assert.That(alert.TextContent).Contains("Sign in again", StringComparison.OrdinalIgnoreCase);
        await Assert.That(alert.TextContent).DoesNotContain("AT Protocol authentication", StringComparison.OrdinalIgnoreCase);
    }

    [Test]
    public async Task AtprotoEvents_WhenInstanceScopeIsSelected_UsesInstanceHalAffordances()
    {
        _settingsService.GetInstanceAsync(Arg.Any<CancellationToken>())
            .Returns(CreateInstanceSettings("update-federation.atproto_events_enabled"));
        _settingsService.UpdateInstanceAsync(
                "federation.atproto_events_enabled",
                "true",
                Arg.Any<CancellationToken>())
            .Returns(new BaseCommandResponseOfGuid { Success = true });

        var cut = RenderComponent(useInstanceScope: true);
        cut.WaitForState(() => cut.Markup.Contains("Enable AT Protocol events", StringComparison.OrdinalIgnoreCase));

        var eventsLabel = cut.FindAll("label").Single(label =>
            label.TextContent.Contains("Enable AT Protocol events", StringComparison.OrdinalIgnoreCase));
        (eventsLabel.QuerySelector("input") ?? throw new InvalidOperationException("AT Protocol events switch input not found."))
            .Change(true);

        await _settingsService.Received(1).UpdateInstanceAsync(
            "federation.atproto_events_enabled",
            "true",
            Arg.Any<CancellationToken>());
        await _settingsService.DidNotReceive().UpdateTenantAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
        await Assert.That(cut.Markup).DoesNotContain("Locked by the instance administrator.", StringComparison.Ordinal);
    }

    [Test]
    public async Task AtprotoEvents_WhenInstanceUnlockAffordanceExists_UnlocksTenantOverride()
    {
        _settingsService.GetInstanceAsync(Arg.Any<CancellationToken>())
            .Returns(CreateInstanceSettings("unlock-federation.atproto_events_enabled"));
        _settingsService.SetInstanceLockAsync(
                "federation.atproto_events_enabled",
                false,
                Arg.Any<CancellationToken>())
            .Returns(new BaseCommandResponseOfGuid { Success = true });

        var cut = RenderComponent(useInstanceScope: true);
        cut.WaitForState(() => cut.Markup.Contains("Unlock tenant override", StringComparison.OrdinalIgnoreCase));

        cut.FindAll("button")
            .Single(button => button.TextContent.Contains("Unlock tenant override", StringComparison.OrdinalIgnoreCase))
            .Click();

        await _settingsService.Received(1).SetInstanceLockAsync(
            "federation.atproto_events_enabled",
            false,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task BackfillControls_WhenEditable_SubmitPlainTenantValues()
    {
        const string enabledKey = "federation.atproto_events_backfill_enabled";
        const string modeKey = "federation.atproto_events_backfill_mode";
        var settings = CreateSettings(eventsCanEdit: true);
        settings.Settings.Add(new EffectiveSettingDto
        {
            Key = enabledKey,
            Value = "false",
            CanEdit = true,
            Source = SettingSource.TenantOverride,
            IsLocked = false,
            IsLockable = true
        });
        settings.Settings.Add(new EffectiveSettingDto
        {
            Key = modeKey,
            Value = "\"downtime_only\"",
            CanEdit = true,
            Source = SettingSource.TenantOverride,
            IsLocked = false,
            IsLockable = true
        });
        _settingsService.GetTenantAsync(Arg.Any<CancellationToken>()).Returns(settings);
        _settingsService.UpdateTenantAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(new BaseCommandResponseOfGuid { Success = true });

        var cut = RenderComponent();
        cut.WaitForState(() => cut.Markup.Contains("Enable inbound event recovery", StringComparison.Ordinal));

        var backfillLabel = cut.FindAll("label").Single(label =>
            label.TextContent.Contains("Enable inbound event recovery", StringComparison.Ordinal));
        (backfillLabel.QuerySelector("input") ?? throw new InvalidOperationException("Backfill switch input not found."))
            .Change(true);
        var modeSelect = cut.FindComponents<MudSelect<string>>()
            .Single(component => component.Instance.Label == "Inbound recovery mode");
        await cut.InvokeAsync(() => modeSelect.Instance.ValueChanged.InvokeAsync("full"));

        await _settingsService.Received(1).UpdateTenantAsync(
            enabledKey,
            "true",
            Arg.Any<CancellationToken>());
        await _settingsService.Received(1).UpdateTenantAsync(
            modeKey,
            "full",
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task BackfillControls_WhenLocked_AreDisabledAndShowReason()
    {
        const string reason = "Inbound recovery is locked by instance policy until the receiver checkpoint has been reviewed by an administrator.";
        var settings = CreateSettings(eventsCanEdit: true);
        settings.Settings.Add(new EffectiveSettingDto
        {
            Key = "federation.atproto_events_backfill_enabled",
            Value = "false",
            CanEdit = false,
            Source = SettingSource.SystemLocked,
            IsLocked = true,
            IsLockable = true,
            Reason = reason
        });
        settings.Settings.Add(new EffectiveSettingDto
        {
            Key = "federation.atproto_events_backfill_mode",
            Value = "\"downtime_only\"",
            CanEdit = false,
            Source = SettingSource.SystemLocked,
            IsLocked = true,
            IsLockable = true,
            Reason = reason
        });
        _settingsService.GetTenantAsync(Arg.Any<CancellationToken>()).Returns(settings);

        var cut = RenderComponent();
        cut.WaitForState(() => cut.Markup.Contains("Enable inbound event recovery", StringComparison.Ordinal));

        var backfillLabel = cut.FindAll("label").Single(label =>
            label.TextContent.Contains("Enable inbound event recovery", StringComparison.Ordinal));
        var backfillInput = backfillLabel.QuerySelector("input")
            ?? throw new InvalidOperationException("Backfill switch input not found.");
        var modeSelect = cut.FindComponents<MudSelect<string>>()
            .Single(component => component.Instance.Label == "Inbound recovery mode");

        await Assert.That(backfillInput.HasAttribute("disabled")).IsTrue();
        await Assert.That(modeSelect.Instance.Disabled).IsTrue();
        await Assert.That(cut.Markup).Contains("Source: System locked", StringComparison.OrdinalIgnoreCase);
        await Assert.That(cut.Markup).Contains(reason, StringComparison.Ordinal);
        await _settingsService.DidNotReceive().UpdateTenantAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ValidationProfile_WhenEditable_SubmitsPlainTenantCode()
    {
        const string key = "federation.atproto_event_validation_profile";
        _settingsService.GetTenantAsync(Arg.Any<CancellationToken>())
            .Returns(CreateSettings(eventsCanEdit: true));
        _settingsService.UpdateTenantAsync(
                key,
                "community_lexicon",
                Arg.Any<CancellationToken>())
            .Returns(new BaseCommandResponseOfGuid { Success = true });

        var cut = RenderComponent();
        cut.WaitForState(() => cut.Markup.Contains("Event creation validation", StringComparison.Ordinal));
        var profileSelect = cut.FindComponents<MudSelect<string>>()
            .Single(component => component.Instance.Label == "Event creation validation");
        await cut.InvokeAsync(() => profileSelect.Instance.ValueChanged.InvokeAsync("community_lexicon"));

        await _settingsService.Received(1).UpdateTenantAsync(
            key,
            "community_lexicon",
            Arg.Any<CancellationToken>());
    }

    private IRenderedComponent<TenantPoliciesSection> RenderComponent(bool useInstanceScope = false) =>
        _ctx.RenderMudComponent<TenantPoliciesSection>(parameters => parameters
            .Add(component => component.Model, new TenantPolicySettingsDto())
            .Add(component => component.UseInstanceScope, useInstanceScope));

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

    private static HalResourceOfSettingGroupResponseDto CreateInstanceSettings(params string[] relations) =>
        new()
        {
            Category = "AtprotoFederation",
            Settings =
            [
                new EffectiveSettingDto
                {
                    Key = "federation.atproto_events_enabled",
                    Value = "false",
                    CanEdit = true,
                    Source = SettingSource.SystemLocked,
                    IsLocked = true,
                    IsLockable = true
                },
                new EffectiveSettingDto
                {
                    Key = "federation.atproto_event_validation_profile",
                    Value = "\"platform\"",
                    CanEdit = true,
                    Source = SettingSource.SystemLocked,
                    IsLocked = true,
                    IsLockable = true
                }
            ],
            _links = relations.ToDictionary(
                relation => relation,
                relation => new HalLink { Href = $"/api/settings/{relation}" },
                StringComparer.Ordinal)
        };
}
