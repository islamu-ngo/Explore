// ABOUTME: bUnit tests verifying lock toggle visibility in instance section components.
// ABOUTME: Ensures lock toggles are hidden in single-tenant mode and visible in multi-tenant mode.

using System.Reflection;
using Explore.Blazor.Client.Models.Analytics;
using MudBlazor;

namespace Explore.Blazor.Client.Tests.Pages.Admin;

public class InstanceSectionLockToggleTests : IDisposable
{
    private readonly BlazorTestContext _ctx;
    private readonly ITenantOnboardingService _tenantOnboardingService;

    public InstanceSectionLockToggleTests()
    {
        _ctx = new BlazorTestContext();
        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Instance Admin", "admin@example.com");
        _ctx.AddMockService<IInstanceOnboardingService>();
        _tenantOnboardingService = _ctx.AddMockService<ITenantOnboardingService>();
        _ctx.AddMockService<ITenantOnboardingService>();
    }

    public void Dispose() => _ctx.Dispose();

    [Test]
    public async Task StorageSection_SingleTenant_NoLockToggle()
    {
        var cut = _ctx.RenderMudComponent<DynamicComponent>(parameters =>
            parameters.Add(component => component.Type, GetComponentType("InstanceStorageSection"))
                      .Add(component => component.Parameters, new Dictionary<string, object>
                      {
                          ["Model"] = new HalResourceOfInstanceStorageSettingsDto(),
                          ["IsSingleTenant"] = true,
                          ["LockForTenants"] = false
                      }));

        await Assert.That(cut.Markup).DoesNotContain("Lock storage settings", StringComparison.OrdinalIgnoreCase);
    }

    [Test]
    public async Task SmtpSection_SingleTenant_NoLockToggle()
    {
        var cut = _ctx.RenderMudComponent<DynamicComponent>(parameters =>
            parameters.Add(component => component.Type, GetComponentType("InstanceSmtpSection"))
                      .Add(component => component.Parameters, new Dictionary<string, object>
                      {
                          ["Model"] = new InstanceSmtpSettingsDto(),
                          ["IsSingleTenant"] = true,
                          ["LockForTenants"] = false
                      }));

        await Assert.That(cut.Markup).DoesNotContain("Lock SMTP settings", StringComparison.OrdinalIgnoreCase);
    }

    [Test]
    public async Task AnalyticsSection_SingleTenant_NoLockToggle()
    {
        var cut = _ctx.RenderMudComponent<DynamicComponent>(parameters =>
            parameters.Add(component => component.Type, GetComponentType("InstanceAnalyticsPrivacySection"))
                      .Add(component => component.Parameters, new Dictionary<string, object>
                      {
                          ["Model"] = new AnalyticsGovernanceSettingsDto(),
                          ["IsSingleTenant"] = true,
                          ["LockForTenants"] = false
                      }));

        await Assert.That(cut.Markup).DoesNotContain("Lock analytics settings", StringComparison.OrdinalIgnoreCase);
    }

    [Test]
    public async Task StorageSection_MultiTenant_HasLockToggle()
    {
        var cut = _ctx.RenderMudComponent<DynamicComponent>(parameters =>
            parameters.Add(component => component.Type, GetComponentType("InstanceStorageSection"))
                      .Add(component => component.Parameters, new Dictionary<string, object>
                      {
                          ["Model"] = new HalResourceOfInstanceStorageSettingsDto(),
                          ["IsSingleTenant"] = false,
                          ["LockForTenants"] = false
                      }));

        await Assert.That(cut.Markup).Contains("Lock storage settings", StringComparison.OrdinalIgnoreCase);
    }

    [Test]
    public async Task StorageSection_LockToggle_SavesImmediatelyAndAnnouncesSuccess()
    {
        bool? captured = null;
        var model = new HalResourceOfInstanceStorageSettingsDto
        {
            _links = new Dictionary<string, HalLink> { ["edit"] = new() { Href = "/storage", Method = "PATCH" } }
        };
        var cut = _ctx.RenderMudComponent<DynamicComponent>(parameters =>
            parameters.Add(component => component.Type, GetComponentType("InstanceStorageSection"))
                .Add(component => component.Parameters, new Dictionary<string, object>
                {
                    ["Model"] = model,
                    ["IsSingleTenant"] = false,
                    ["LockForTenants"] = false,
                    ["SaveLockAsync"] = new Func<bool, Task<bool>>(value =>
                    {
                        captured = value;
                        return Task.FromResult(true);
                    })
                }));

        await cut.InvokeAsync(() => cut.FindComponents<MudSwitch<bool>>().Last().Instance.ValueChanged.InvokeAsync(true));

        await Assert.That(captured).IsTrue();
        await Assert.That(cut.Find("[role='status']").TextContent).Contains("Storage tenant lock saved.");
    }

    [Test]
    public async Task SmtpSection_LockToggle_WhenSaveFails_AnnouncesAuthoritativeRestore()
    {
        var cut = _ctx.RenderMudComponent<DynamicComponent>(parameters =>
            parameters.Add(component => component.Type, GetComponentType("InstanceSmtpSection"))
                .Add(component => component.Parameters, new Dictionary<string, object>
                {
                    ["Model"] = new InstanceSmtpSettingsDto(),
                    ["IsSingleTenant"] = false,
                    ["LockForTenants"] = false,
                    ["SaveLockAsync"] = new Func<bool, Task<bool>>(_ => Task.FromResult(false))
                }));

        await cut.InvokeAsync(() => cut.FindComponents<MudSwitch<bool>>().Last().Instance.ValueChanged.InvokeAsync(true));

        await Assert.That(cut.Find("[role='alert']").TextContent).Contains("latest value was restored");
    }

    [Test]
    public async Task ModulesSection_ToggleSendsOnePropertyAndAnnouncesSavedState()
    {
        ModuleSettingsDto? captured = null;
        var cut = _ctx.RenderMudComponent<DynamicComponent>(parameters =>
            parameters.Add(component => component.Type, GetComponentType("InstanceModulesSection"))
                      .Add(component => component.Parameters, new Dictionary<string, object>
                      {
                          ["Model"] = new ModuleSettingsDto(),
                          ["EventPolicy"] = new EventPolicyDto(),
                          ["OrganizationPolicy"] = new OrganizationPolicyDto(),
                          ["SaveModuleSettingsAsync"] = new Func<ModuleSettingsDto, Task<BaseCommandResponseOfGuid>>(patch =>
                          {
                              captured = patch;
                              return Task.FromResult(new BaseCommandResponseOfGuid { Success = true });
                          }),
                          ["SaveEventPolicyAsync"] = new Func<EventPolicyDto, Task<BaseCommandResponseOfGuid>>(_ =>
                              Task.FromResult(new BaseCommandResponseOfGuid { Success = true })),
                          ["SaveOrganizationPolicyAsync"] = new Func<OrganizationPolicyDto, Task<BaseCommandResponseOfGuid>>(_ =>
                              Task.FromResult(new BaseCommandResponseOfGuid { Success = true }))
                      }));

        await cut.InvokeAsync(() =>
            cut.FindComponents<MudSwitch<bool>>()[0].Instance.ValueChanged.InvokeAsync(true));

        await Assert.That(captured).IsNotNull();
        await Assert.That(captured!.EnableIslamicModule).IsTrue();
        await Assert.That(captured.EnableTechModule).IsNull();
        await Assert.That(cut.Markup).Contains("Module settings saved.", StringComparison.Ordinal);
    }

    [Test]
    public async Task BrandingSection_ToggleSendsOnePropertyAndAnnouncesSavedState()
    {
        BrandingSettingsDto? captured = null;
        var cut = _ctx.RenderMudComponent<DynamicComponent>(parameters =>
            parameters.Add(component => component.Type, GetComponentType("InstanceBrandingSection"))
                      .Add(component => component.Parameters, new Dictionary<string, object>
                      {
                          ["Model"] = new BrandingSettingsDto(),
                          ["IsSingleTenant"] = false,
                          ["SaveBrandingAsync"] = new Func<BrandingSettingsDto, Task<BaseCommandResponseOfGuid>>(patch =>
                          {
                              captured = patch;
                              return Task.FromResult(new BaseCommandResponseOfGuid { Success = true });
                          })
                      }));

        await cut.InvokeAsync(() =>
            cut.FindComponents<MudSwitch<bool>>()[0].Instance.ValueChanged.InvokeAsync(true));

        await Assert.That(captured).IsNotNull();
        await Assert.That(captured!.LockTenantBrandDisplayName).IsTrue();
        await Assert.That(captured.LockTenantBrandLogoUrl).IsNull();
        await Assert.That(cut.Markup).Contains("Branding settings saved.", StringComparison.Ordinal);
    }

    [Test]
    public async Task DomainSection_ToggleSendsOnePropertyAndAnnouncesSavedState()
    {
        DomainSettingsDto? captured = null;
        var cut = _ctx.RenderMudComponent<DynamicComponent>(parameters =>
            parameters.Add(component => component.Type, GetComponentType("InstanceDomainSection"))
                      .Add(component => component.Parameters, new Dictionary<string, object>
                      {
                          ["Model"] = new DomainSettingsDto(),
                          ["SaveDomainAsync"] = new Func<DomainSettingsDto, Task<BaseCommandResponseOfGuid>>(patch =>
                          {
                              captured = patch;
                              return Task.FromResult(new BaseCommandResponseOfGuid { Success = true });
                          })
                      }));

        await cut.InvokeAsync(() =>
            cut.FindComponents<MudSwitch<bool>>()[0].Instance.ValueChanged.InvokeAsync(true));

        await Assert.That(captured).IsNotNull();
        await Assert.That(captured!.AllowTenantCustomDomains).IsTrue();
        await Assert.That(captured.LockTenantSubdomain).IsNull();
        await Assert.That(cut.Markup).Contains("Domain settings saved.", StringComparison.Ordinal);
    }

    [Test]
    public async Task AnalyticsSection_ToggleSendsOnePropertyAndAnnouncesSavedState()
    {
        AnalyticsGovernanceSettingsDto? captured = null;
        var cut = _ctx.RenderMudComponent<DynamicComponent>(parameters =>
            parameters.Add(component => component.Type, GetComponentType("InstanceAnalyticsPrivacySection"))
                      .Add(component => component.Parameters, new Dictionary<string, object>
                      {
                          ["Model"] = new AnalyticsGovernanceSettingsDto(),
                          ["IsSingleTenant"] = true,
                          ["SaveAnalyticsAsync"] = new Func<AnalyticsGovernanceSettingsDto, Task<BaseCommandResponseOfGuid>>(patch =>
                          {
                              captured = patch;
                              return Task.FromResult(new BaseCommandResponseOfGuid { Success = true });
                          }),
                          ["SaveDelegationAsync"] = new Func<TenantDelegationSettingsDto, Task<BaseCommandResponseOfGuid>>(_ =>
                              Task.FromResult(new BaseCommandResponseOfGuid { Success = true }))
                      }));

        await cut.InvokeAsync(() =>
            cut.FindComponents<MudSwitch<bool>>()[0].Instance.ValueChanged.InvokeAsync(true));

        await Assert.That(captured).IsNotNull();
        await Assert.That(captured!.GlobalDisableClientTracking).IsTrue();
        await Assert.That(captured.CookieConsentEnabled).IsNull();
        await Assert.That(cut.Markup).Contains("Analytics settings saved.", StringComparison.Ordinal);
    }

    [Test]
    public async Task AiSection_ToggleSendsOnePropertyAndAnnouncesSavedState()
    {
        AiAssistantGovernanceSettingsDto? captured = null;
        var cut = _ctx.RenderMudComponent<DynamicComponent>(parameters =>
            parameters.Add(component => component.Type, GetComponentType("InstanceAiSection"))
                      .Add(component => component.Parameters, new Dictionary<string, object>
                      {
                          ["AiAssistant"] = new AiAssistantGovernanceSettingsDto(),
                          ["SaveAiAssistantAsync"] = new Func<AiAssistantGovernanceSettingsDto, Task<BaseCommandResponseOfGuid>>(patch =>
                          {
                              captured = patch;
                              return Task.FromResult(new BaseCommandResponseOfGuid { Success = true });
                          }),
                          ["SaveAiProviderConfigurationAsync"] = new Func<AiAssistantProviderConfigurationWriteDto, Task<BaseCommandResponseOfGuid>>(_ =>
                              Task.FromResult(new BaseCommandResponseOfGuid { Success = true }))
                      }));

        await cut.InvokeAsync(() =>
            cut.FindComponents<MudSwitch<bool>>()[0].Instance.ValueChanged.InvokeAsync(true));

        await Assert.That(captured).IsNotNull();
        await Assert.That(captured!.Enabled).IsTrue();
        await Assert.That(captured.Provider).IsNull();
        await Assert.That(cut.Find("[role='status']").TextContent).Contains("AI Assistant setting saved.", StringComparison.Ordinal);
    }

    [Test]
    public async Task AiSection_ProviderFieldsRemainExplicitAndSaveAsOneCoupledPatch()
    {
        AiAssistantProviderConfigurationWriteDto? captured = null;
        var model = new AiAssistantGovernanceSettingsDto
        {
            Enabled = true,
            Provider = "openai-compatible",
            EndpointUrl = "https://ai.example.test/v1",
            ModelId = "model-a",
            AllowedModelIds = ["model-a", "model-b"]
        };
        var cut = _ctx.RenderMudComponent<DynamicComponent>(parameters =>
            parameters.Add(component => component.Type, GetComponentType("InstanceAiSection"))
                      .Add(component => component.Parameters, new Dictionary<string, object>
                      {
                          ["AiAssistant"] = model,
                          ["SaveAiAssistantAsync"] = new Func<AiAssistantGovernanceSettingsDto, Task<BaseCommandResponseOfGuid>>(_ =>
                              Task.FromResult(new BaseCommandResponseOfGuid { Success = true })),
                          ["SaveAiProviderConfigurationAsync"] = new Func<AiAssistantProviderConfigurationWriteDto, Task<BaseCommandResponseOfGuid>>(patch =>
                          {
                              captured = patch;
                              return Task.FromResult(new BaseCommandResponseOfGuid { Success = true });
                          })
                      }));

        await Assert.That(captured).IsNull();
        var apiKeyField = cut.FindComponents<MudTextField<string>>().Single(field =>
            field.Instance.Label?.Contains("API key", StringComparison.Ordinal) == true);
        await cut.InvokeAsync(() => apiKeyField.Instance.ValueChanged.InvokeAsync("replacement-key"));
        cut.FindAll("button").Single(button =>
            button.TextContent.Contains("Save provider configuration", StringComparison.Ordinal)).Click();

        await Assert.That(captured).IsNotNull();
        await Assert.That(captured!.Provider).IsEqualTo("openai-compatible");
        await Assert.That(captured.EndpointUrl).IsEqualTo("https://ai.example.test/v1");
        await Assert.That(captured.ApiKey).IsEqualTo("replacement-key");
        await Assert.That(captured.ModelId).IsEqualTo("model-a");
        await Assert.That(captured.AllowedModelIds).IsEquivalentTo(["model-a", "model-b"]);
        await Assert.That(cut.Find("[role='status']").TextContent).Contains("AI provider configuration saved.", StringComparison.Ordinal);
    }

    [Test]
    public async Task BrandingSection_TextSavesOnBlurAsOneProperty()
    {
        BrandingSettingsDto? captured = null;
        var cut = _ctx.RenderMudComponent<DynamicComponent>(parameters =>
            parameters.Add(component => component.Type, GetComponentType("InstanceBrandingSection"))
                      .Add(component => component.Parameters, new Dictionary<string, object>
                      {
                          ["Model"] = new BrandingSettingsDto(),
                          ["IsSingleTenant"] = true,
                          ["SaveBrandingAsync"] = new Func<BrandingSettingsDto, Task<BaseCommandResponseOfGuid>>(patch =>
                          {
                              captured = patch;
                              return Task.FromResult(new BaseCommandResponseOfGuid { Success = true });
                          })
                      }));
        var field = cut.FindComponents<MudTextField<string>>()
            .Single(component => component.Instance.Label == "Brand Display Name");

        await cut.InvokeAsync(() => field.Instance.ValueChanged.InvokeAsync("Authoritative Brand"));
        await Assert.That(captured).IsNull();
        field.Find("input").Blur();

        await Assert.That(captured).IsNotNull();
        await Assert.That(captured!.DefaultBrandDisplayName).IsEqualTo("Authoritative Brand");
        await Assert.That(captured.DefaultBrandLogoUrl).IsNull();
    }

    [Test]
    public async Task FooterGovernanceSection_ToggleSendsOnePropertyAndAnnouncesSavedState()
    {
        FooterGovernanceSettingsDto? captured = null;
        var cut = _ctx.RenderMudComponent<DynamicComponent>(parameters =>
            parameters.Add(component => component.Type, GetComponentType("InstanceFooterGovernanceSection"))
                      .Add(component => component.Parameters, new Dictionary<string, object>
                      {
                          ["Model"] = new FooterGovernanceSettingsDto(),
                          ["SaveFooterGovernanceAsync"] = new Func<FooterGovernanceSettingsDto, Task<BaseCommandResponseOfGuid>>(patch =>
                          {
                              captured = patch;
                              return Task.FromResult(new BaseCommandResponseOfGuid { Success = true });
                          })
                      }));

        await cut.InvokeAsync(() =>
            cut.FindComponents<MudSwitch<bool>>()[0].Instance.ValueChanged.InvokeAsync(true));

        await Assert.That(captured).IsNotNull();
        await Assert.That(captured!.LockTenantTemplate).IsTrue();
        await Assert.That(captured.LockTenantLinkGroups).IsNull();
        await Assert.That(cut.Find("[role='status']").TextContent).Contains("Footer governance saved.", StringComparison.Ordinal);
    }

    [Test]
    public async Task AiSection_EndpointChangeIgnoresStaleModelDiscoveryResponse()
    {
        var response = new TaskCompletionSource<IReadOnlyList<AiAssistantModelDto>>(TaskCreationOptions.RunContinuationsAsynchronously);
        _tenantOnboardingService.GetAiModelsAsync("https://first.example.test/v1", string.Empty)
            .Returns(response.Task);
        var model = new AiAssistantGovernanceSettingsDto
        {
            Enabled = true,
            EndpointUrl = "https://first.example.test/v1"
        };
        var cut = _ctx.RenderMudComponent<DynamicComponent>(parameters =>
            parameters.Add(component => component.Type, GetComponentType("InstanceAiSection"))
                      .Add(component => component.Parameters, new Dictionary<string, object>
                      {
                          ["AiAssistant"] = model,
                          ["SaveAiAssistantAsync"] = new Func<AiAssistantGovernanceSettingsDto, Task<BaseCommandResponseOfGuid>>(_ =>
                              Task.FromResult(new BaseCommandResponseOfGuid { Success = true }))
                      }));
        object component = cut.Instance.Instance
            ?? throw new InvalidOperationException("Dynamic component did not expose the AI section instance.");
        MethodInfo loadModels = component.GetType().GetMethod("LoadModelsAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("AI model discovery method was not found.");
        Task pendingLoad = Task.CompletedTask;
        await cut.InvokeAsync(() =>
        {
            pendingLoad = (Task)loadModels.Invoke(component, null)!;
        });
        var endpointField = cut.FindComponents<MudTextField<string>>()
            .Single(field => field.Instance.Label == "Endpoint URL");

        await cut.InvokeAsync(() => endpointField.Instance.ValueChanged.InvokeAsync("https://second.example.test/v1"));
        response.SetResult([new AiAssistantModelDto { Id = "stale-model" }]);
        await pendingLoad;

        await Assert.That(model.EndpointUrl).IsEqualTo("https://second.example.test/v1");
        await Assert.That(string.IsNullOrEmpty(model.ModelId)).IsTrue();
        await Assert.That(cut.Markup).DoesNotContain("stale-model", StringComparison.Ordinal);
        await Assert.That(cut.FindAll("button").Single(button =>
            button.TextContent.Contains("Load models", StringComparison.Ordinal)).HasAttribute("disabled")).IsFalse();
    }

    private static Type GetComponentType(string componentName)
    {
        var componentType = typeof(IInstanceOnboardingService).Assembly
            .GetTypes()
            .FirstOrDefault(type => type.Name == componentName && typeof(IComponent).IsAssignableFrom(type));

        return componentType ?? throw new InvalidOperationException($"Could not find component type '{componentName}'.");
    }
}
