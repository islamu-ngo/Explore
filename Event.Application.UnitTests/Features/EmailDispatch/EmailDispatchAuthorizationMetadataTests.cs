// ABOUTME: Unit tests for EmailDispatch admin request authorization metadata.
// ABOUTME: Prevents email dispatch status/control operations from bypassing MediatR resource authorization.

using System.Reflection;
using Explore.Application.Authorization;
using Explore.Application.Features.EmailDispatch.Requests.Commands;
using Explore.Application.Features.EmailDispatch.Requests.Queries;

namespace Event.Application.UnitTests.Features.EmailDispatch;

public sealed class EmailDispatchAuthorizationMetadataTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid OutboxId = Guid.NewGuid();

    public static IEnumerable<(Type RequestType, string ExpectedAction)> EmailDispatchRequests()
    {
        yield return (typeof(GetEmailDispatchStatusQuery), AuthorizationActions.EmailDispatches.View);
        yield return (typeof(SetEmailDispatchTenantPauseStateCommand), AuthorizationActions.EmailDispatches.ManageTenant);
        yield return (typeof(ParkEmailDispatchCommand), AuthorizationActions.EmailDispatches.Park);
        yield return (typeof(ReplayEmailDispatchCommand), AuthorizationActions.EmailDispatches.Replay);
    }

    [Test]
    [MethodDataSource(nameof(EmailDispatchRequests))]
    public async Task EmailDispatchAdminRequestsRequireEmailDispatchAuthorization(
        (Type RequestType, string ExpectedAction) testCase)
    {
        var attribute = testCase.RequestType.GetCustomAttribute<AuthorizeResourceAttribute>();

        await Assert.That(attribute).IsNotNull();
        await Assert.That(attribute!.Resource).IsEqualTo(ResourceKinds.EmailDispatch);
        await Assert.That(attribute.Action).IsEqualTo(testCase.ExpectedAction);
        await Assert.That(typeof(ISecureRequest).IsAssignableFrom(testCase.RequestType)).IsTrue();
    }

    public static IEnumerable<(ISecureRequest Request, string ExpectedResourceId, string? ExpectedOperation)> AuthorizedEmailDispatchRequests()
    {
        yield return (new GetEmailDispatchStatusQuery { TenantId = TenantId }, TenantId.ToString("D"), null);
        yield return (new SetEmailDispatchTenantPauseStateCommand { TenantId = TenantId, IsPaused = true }, TenantId.ToString("D"), "pause");
        yield return (new SetEmailDispatchTenantPauseStateCommand { TenantId = TenantId, IsPaused = false }, TenantId.ToString("D"), "resume");
        yield return (new ParkEmailDispatchCommand { TenantId = TenantId, OutboxId = OutboxId, Reason = "unsafe" }, OutboxId.ToString("D"), null);
        yield return (new ReplayEmailDispatchCommand { TenantId = TenantId, OutboxId = OutboxId }, OutboxId.ToString("D"), null);
    }

    [Test]
    [MethodDataSource(nameof(AuthorizedEmailDispatchRequests))]
    public async Task EmailDispatchAdminRequestsExposeTenantAndResourceAuthorizationContext(
        (ISecureRequest Request, string ExpectedResourceId, string? ExpectedOperation) testCase)
    {
        await Assert.That(testCase.Request.ResourceId).IsEqualTo(testCase.ExpectedResourceId);
        await Assert.That(testCase.Request.ResourceAttributes).IsNotNull();
        await Assert.That(testCase.Request.ResourceAttributes!["tenantId"]).IsEqualTo(TenantId.ToString("D"));

        if (testCase.Request is ParkEmailDispatchCommand or ReplayEmailDispatchCommand)
        {
            await Assert.That(testCase.Request.ResourceAttributes["outboxId"]).IsEqualTo(OutboxId.ToString("D"));
        }

        if (testCase.ExpectedOperation is not null)
        {
            await Assert.That(testCase.Request.ResourceAttributes["emailDispatchOperation"]).IsEqualTo(testCase.ExpectedOperation);
        }
    }
}
