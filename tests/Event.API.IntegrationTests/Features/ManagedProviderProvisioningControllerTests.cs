// ABOUTME: API controller contract tests for managed-provider provisioning entrypoint.
// ABOUTME: Verifies instance-admin gating and thin MediatR dispatch for trusted provisioning.

using Explore.API.Controllers;
using Explore.Application.Contracts.Identity;
using Explore.Application.DTOs.ManagedProviderProvisioning;
using Explore.Application.Features.ManagedProviderProvisioning.Requests.Commands;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Event.Api.IntegrationTests.Features;

public sealed class ManagedProviderProvisioningControllerTests
{
    [Test]
    public async Task EnsureClientProvisioned_WhenCallerIsNotInstanceAdmin_ReturnsForbiddenWithoutDispatch()
    {
        var mediator = Substitute.For<IMediator>();
        var adminContext = Substitute.For<IAdminContext>();
        adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        var controller = new ManagedProviderProvisioningController(mediator, adminContext)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        ActionResult<BaseCommandResponse<ManagedProviderClientProvisioningResultDto>> actionResult =
            await controller.EnsureClientProvisioned(NewDto(), CancellationToken.None);

        var objectResult = actionResult.Result as ObjectResult;
        await Assert.That(objectResult).IsNotNull();
        await Assert.That(objectResult!.StatusCode).IsEqualTo(StatusCodes.Status403Forbidden);
        await mediator.DidNotReceive().Send(Arg.Any<EnsureManagedProviderClientProvisionedCommand>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task EnsureClientProvisioned_WhenCallerIsInstanceAdmin_DispatchesCommandAndReturnsOk()
    {
        var resultDto = new ManagedProviderClientProvisioningResultDto
        {
            TenantId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            UserActorId = Guid.NewGuid(),
            UserExternalLoginId = Guid.NewGuid(),
            TenantUserRoleGrantId = Guid.NewGuid(),
        };
        var response = BaseCommandResponse.Success(resultDto, "Provisioned");
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<EnsureManagedProviderClientProvisionedCommand>(), Arg.Any<CancellationToken>())
            .Returns(response);
        var adminContext = Substitute.For<IAdminContext>();
        adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(true);
        var controller = new ManagedProviderProvisioningController(mediator, adminContext);
        var dto = NewDto();

        ActionResult<BaseCommandResponse<ManagedProviderClientProvisioningResultDto>> actionResult =
            await controller.EnsureClientProvisioned(dto, CancellationToken.None);

        var ok = actionResult.Result as OkObjectResult;
        await Assert.That(ok).IsNotNull();
        await Assert.That(ok!.Value).IsEqualTo(response);
        await mediator.Received(1).Send(
            Arg.Is<EnsureManagedProviderClientProvisionedCommand>(command => command.ProvisioningDto == dto),
            Arg.Any<CancellationToken>());
    }

    private static ManagedProviderClientProvisioningDto NewDto() =>
        new()
        {
            ProviderKey = "erp",
            ExternalSystem = "crmworx",
            ExternalCustomerId = "customer-42",
            TenantFullName = "CRM Worx Customer",
            TenantSlug = "crm-worx-customer",
            ExternalAdmin = new ManagedProviderExternalAdminDto
            {
                IdentityProvider = "keycloak",
                Subject = "admin-subject",
                Email = "admin@example.com",
                FirstName = "Amina",
                LastName = "Admin",
            },
        };
}
