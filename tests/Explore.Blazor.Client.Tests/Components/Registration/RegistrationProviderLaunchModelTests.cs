// ABOUTME: Contract tests for attendee registration-provider launch state.
// ABOUTME: Proves same-origin BFF launch URLs and server-polled completion authority.

using Explore.Blazor.Client.Components.Registration.FormRenderer;
using Explore.Blazor.Client.Components.Registration.ProviderLaunch;

namespace Explore.Blazor.Client.Tests.Components.Registration;

public sealed class RegistrationProviderLaunchModelTests
{
    private readonly IRegistrationProviderIntegrationService _integrationService = Substitute.For<IRegistrationProviderIntegrationService>();
    private readonly INativeRegistrationFormService _nativeRegistrationFormService = Substitute.For<INativeRegistrationFormService>();
    private readonly IRegistrationOrderService _registrationOrderService = Substitute.For<IRegistrationOrderService>();
    private readonly IBrowserActionInterop _browserActions = Substitute.For<IBrowserActionInterop>();
    private readonly RegistrationProviderLaunchState _model;

    public RegistrationProviderLaunchModelTests()
    {
        _model = new RegistrationProviderLaunchState(_integrationService, _nativeRegistrationFormService, _registrationOrderService, _browserActions);
    }

    [Test]
    public async Task InitializeAsync_BuildsOnlySameOriginSixIdEmbedUrl()
    {
        var context = CreateContext();
        _integrationService.GetLaunchDescriptorAsync(context.Lineage, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new HalResourceOfRegistrationProviderLaunchDescriptorDto()));
        _nativeRegistrationFormService.GetRequirementsAsync(context.EventId, context.OrderId, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<NativeRegistrationRequirementCollectionView?>(IncompleteRequirements(context.Lineage.RequirementId)));
        _registrationOrderService.GetCurrentAsync(context.EventId, context.OrderId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<HalResourceOfRegistrationOrderDto?>(new HalResourceOfRegistrationOrderDto()));

        await _model.InitializeAsync(context);

        await Assert.That(_model.EmbedUrl).IsEqualTo(
            $"/bff/registration-provider-embed/tenants/{context.Lineage.TenantId:D}/events/{context.Lineage.EventId:D}/workflows/{context.Lineage.WorkflowId:D}/requirements/{context.Lineage.RequirementId:D}/channels/{context.Lineage.ChannelId:D}/bindings/{context.Lineage.BindingId:D}");
        await Assert.That(_model.EmbedUrl).DoesNotContain("?");
        await Assert.That(_model.IsComplete).IsFalse();
    }

    [Test]
    public async Task IframeCallbacks_DoNotMarkRequirementComplete()
    {
        var context = CreateContext();
        _integrationService.GetLaunchDescriptorAsync(context.Lineage, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new HalResourceOfRegistrationProviderLaunchDescriptorDto()));
        _nativeRegistrationFormService.GetRequirementsAsync(context.EventId, context.OrderId, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<NativeRegistrationRequirementCollectionView?>(IncompleteRequirements(context.Lineage.RequirementId)));
        _registrationOrderService.GetCurrentAsync(context.EventId, context.OrderId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<HalResourceOfRegistrationOrderDto?>(new HalResourceOfRegistrationOrderDto()));

        await _model.InitializeAsync(context);
        await _model.OnIframeLoadedAsync();
        await _model.OnIframeNavigatedAsync();

        await Assert.That(_model.IframeLoaded).IsTrue();
        await Assert.That(_model.IsComplete).IsFalse();
    }

    [Test]
    public async Task PollAsync_MarksCompleteOnlyFromAuthoritativeRequirementStatus()
    {
        var context = CreateContext();
        _integrationService.GetLaunchDescriptorAsync(context.Lineage, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new HalResourceOfRegistrationProviderLaunchDescriptorDto()));
        _nativeRegistrationFormService.GetRequirementsAsync(context.EventId, context.OrderId, null, Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult<NativeRegistrationRequirementCollectionView?>(IncompleteRequirements(context.Lineage.RequirementId)),
                Task.FromResult<NativeRegistrationRequirementCollectionView?>(CompleteRequirements(context.Lineage.RequirementId)));
        _registrationOrderService.GetCurrentAsync(context.EventId, context.OrderId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<HalResourceOfRegistrationOrderDto?>(new HalResourceOfRegistrationOrderDto()));

        await _model.InitializeAsync(context);
        await _model.PollAsync();

        await Assert.That(_model.IsComplete).IsTrue();
        await _nativeRegistrationFormService.Received(2).GetRequirementsAsync(context.EventId, context.OrderId, null, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task OpenAuthorizedNewTabAsync_UsesSameOriginBffUrl()
    {
        var context = CreateContext();
        _integrationService.GetLaunchDescriptorAsync(context.Lineage, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new HalResourceOfRegistrationProviderLaunchDescriptorDto()));
        _nativeRegistrationFormService.GetRequirementsAsync(context.EventId, context.OrderId, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<NativeRegistrationRequirementCollectionView?>(IncompleteRequirements(context.Lineage.RequirementId)));
        _registrationOrderService.GetCurrentAsync(context.EventId, context.OrderId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<HalResourceOfRegistrationOrderDto?>(new HalResourceOfRegistrationOrderDto()));
        _browserActions.OpenSameOriginNewTabAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));

        await _model.InitializeAsync(context);
        var opened = await _model.OpenAuthorizedNewTabAsync();

        await Assert.That(opened).IsTrue();
        await _browserActions.Received(1).OpenSameOriginNewTabAsync(_model.EmbedUrl!, Arg.Any<CancellationToken>());
    }

    private static RegistrationProviderLaunchContext CreateContext() => new(
        Guid.CreateVersion7(),
        Guid.CreateVersion7(),
        new RegistrationProviderLaunchLineage(
            Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(),
            Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7()));

    private static NativeRegistrationRequirementCollectionView IncompleteRequirements(Guid requirementId) => Requirements(requirementId, false);

    private static NativeRegistrationRequirementCollectionView CompleteRequirements(Guid requirementId) => Requirements(requirementId, true);

    private static NativeRegistrationRequirementCollectionView Requirements(Guid requirementId, bool complete) => new(
        [new NativeRegistrationLaunchDescriptorView(
            requirementId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            false,
            [],
            new RegistrationRequirementProgressView(1, complete ? 1 : 0, 0, complete ? 0 : 1, complete))],
        new Dictionary<string, HalLink>());
}
