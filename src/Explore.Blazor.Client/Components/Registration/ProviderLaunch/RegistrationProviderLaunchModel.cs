// ABOUTME: Attendee-side provider launch state contract for external registration channels.
// ABOUTME: Uses server HAL/DTO status as authority; iframe browser events never complete a requirement.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Contracts.Interop;
using Explore.Blazor.Client.Contracts.Services;
using Explore.Blazor.Client.Components.Registration.FormRenderer;

namespace Explore.Blazor.Client.Components.Registration.ProviderLaunch;

public sealed record RegistrationProviderLaunchContext(
    Guid EventId,
    Guid OrderId,
    RegistrationProviderLaunchLineage Lineage,
    GuestRegistrationOrderCapability? GuestCapability = null);

public sealed record RegistrationProviderPollingSnapshot(
    NativeRegistrationRequirementCollectionView? Requirements,
    object? OrderResource);

public sealed class RegistrationProviderLaunchState(
    IRegistrationProviderIntegrationService integrationService,
    INativeRegistrationFormService nativeRegistrationFormService,
    IRegistrationOrderService registrationOrderService,
    IBrowserActionInterop browserActions)
{
    public RegistrationProviderLaunchContext? Context { get; private set; }
    public HalResourceOfRegistrationProviderLaunchDescriptorDto? Descriptor { get; private set; }
    public string? EmbedUrl { get; private set; }
    public RegistrationProviderPollingSnapshot? Snapshot { get; private set; }
    public bool IframeLoaded { get; private set; }
    public bool IsComplete => Context is { } context && Snapshot?.Requirements?.Requirements
        .SingleOrDefault(requirement => requirement.RequirementId == context.Lineage.RequirementId)
        ?.Progress.IsComplete == true;

    public static bool CanLaunch(IReadOnlyDictionary<string, HalLink>? links) =>
        links?.ContainsKey("launch-descriptor") == true;

    public static string BuildEmbedUrl(RegistrationProviderLaunchLineage lineage) =>
        $"/bff/registration-provider-embed/tenants/{lineage.TenantId:D}/events/{lineage.EventId:D}/workflows/{lineage.WorkflowId:D}/requirements/{lineage.RequirementId:D}/channels/{lineage.ChannelId:D}/bindings/{lineage.BindingId:D}";

    public async Task InitializeAsync(RegistrationProviderLaunchContext context, CancellationToken cancellationToken = default)
    {
        Context = context;
        Descriptor = await integrationService.GetLaunchDescriptorAsync(context.Lineage, cancellationToken);
        EmbedUrl = BuildEmbedUrl(context.Lineage);
        await PollAsync(cancellationToken);
    }

    public Task<bool> OpenAuthorizedNewTabAsync(CancellationToken cancellationToken = default) =>
        EmbedUrl is { Length: > 0 }
            ? browserActions.OpenSameOriginNewTabAsync(EmbedUrl, cancellationToken)
            : Task.FromResult(false);

    public async Task PollAsync(CancellationToken cancellationToken = default)
    {
        if (Context is not { } context)
        {
            return;
        }

        var requirements = await nativeRegistrationFormService.GetRequirementsAsync(
            context.EventId,
            context.OrderId,
            context.GuestCapability,
            cancellationToken);
        object? order = context.GuestCapability is { } guest
            ? await registrationOrderService.GetGuestAsync(context.EventId, context.OrderId, guest, cancellationToken)
            : await registrationOrderService.GetCurrentAsync(context.EventId, context.OrderId, cancellationToken);
        Snapshot = new RegistrationProviderPollingSnapshot(requirements, order);
    }

    public Task OnIframeLoadedAsync()
    {
        IframeLoaded = true;
        return Task.CompletedTask;
    }

    public Task OnIframeNavigatedAsync() => Task.CompletedTask;
}
