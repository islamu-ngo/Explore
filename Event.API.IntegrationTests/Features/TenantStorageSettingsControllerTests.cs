// ABOUTME: API controller tests for tenant storage settings routes.
// ABOUTME: Verifies CQRS dispatch and HTTP result mapping for tenant storage administration.

using Event.Api.IntegrationTests.Fixtures;
using Explore.API.Controllers;
using Explore.API.Hateoas;
using Explore.Application.Contracts.Identity;
using Explore.Application.DTOs.Tenant;
using Explore.Application.Features.TenantStorageSettings.Requests.Commands;
using Explore.Application.Features.TenantStorageSettings.Requests.Queries;
using Explore.Application.Hateoas;
using Explore.Application.Responses;
using Explore.Domain;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Event.Api.IntegrationTests.Features;

[Category(TestCategories.Fast)]
[Category("TenantStorageSettings")]
public sealed class TenantStorageSettingsControllerTests
{
    [Test]
    public async Task GetStorageSettings_ReturnsMediatorSettings()
    {
        var mediator = Substitute.For<IMediator>();
        var assembler = Substitute.For<IResourceAssembler<TenantStorageSettingsDto, TenantStorageSettingsDto>>();
        var settings = new TenantStorageSettingsDto
        {
            Provider = StorageProviders.Local,
            MaxUploadBytes = 4096,
            TenantQuotaBytes = 8192
        };
        var halResource = new HalResource<TenantStorageSettingsDto>(settings);
        mediator.Send(Arg.Any<GetTenantStorageSettingsQuery>(), Arg.Any<CancellationToken>())
            .Returns(settings);
        assembler.ToResource(settings, Arg.Any<HttpContext>()).Returns(halResource);
        var controller = CreateController(mediator, storageSettingsAssembler: assembler);

        var result = await controller.GetStorageSettings(CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        await Assert.That(ok).IsNotNull();
        await Assert.That(ok!.Value).IsEqualTo(halResource);
        await mediator.Received(1).Send(Arg.Any<GetTenantStorageSettingsQuery>(), Arg.Any<CancellationToken>());
        await assembler.Received(1).ToResource(settings, Arg.Any<HttpContext>());
    }

    [Test]
    public async Task UpdateStorageSettings_WhenMediatorSucceeds_ReturnsOk()
    {
        var userId = Guid.NewGuid();
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<UpdateTenantStorageSettingsCommand>(), Arg.Any<CancellationToken>())
            .Returns(new BaseCommandResponse<Guid>
            {
                Success = true,
                Id = Guid.NewGuid(),
                Message = "Tenant storage settings updated successfully."
            });
        var controller = CreateController(mediator, userId);

        var result = await controller.UpdateStorageSettings(CreateSettings(), CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        await Assert.That(ok).IsNotNull();
        var response = ok!.Value as BaseCommandResponse<Guid>;
        await Assert.That(response).IsNotNull();
        await Assert.That(response!.Success).IsTrue();
        await mediator.Received(1).Send(
            Arg.Is<UpdateTenantStorageSettingsCommand>(command => command != null && command.UserId == userId),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UpdateStorageSettings_WhenMediatorReportsPolicyFailure_ReturnsBadRequest()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<UpdateTenantStorageSettingsCommand>(), Arg.Any<CancellationToken>())
            .Returns(new BaseCommandResponse<Guid>
            {
                Success = false,
                FailureCode = "StorageTenantOverridesLocked",
                Message = "Tenant storage settings are locked by instance policy."
            });
        var controller = CreateController(mediator, Guid.NewGuid());

        var result = await controller.UpdateStorageSettings(CreateSettings(), CancellationToken.None);

        var badRequest = result.Result as BadRequestObjectResult;
        await Assert.That(badRequest).IsNotNull();
        var response = badRequest!.Value as BaseCommandResponse<Guid>;
        await Assert.That(response).IsNotNull();
        await Assert.That(response!.FailureCode).IsEqualTo("StorageTenantOverridesLocked");
    }

    [Test]
    public async Task UpdateStorageSettings_WhenMediatorReportsAdminFailure_ReturnsForbidden()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<UpdateTenantStorageSettingsCommand>(), Arg.Any<CancellationToken>())
            .Returns(new BaseCommandResponse<Guid>
            {
                Success = false,
                Message = "Only tenant administrators or instance administrators can update tenant storage settings."
            });
        var controller = CreateController(mediator, Guid.NewGuid());

        var result = await controller.UpdateStorageSettings(CreateSettings(), CancellationToken.None);

        await Assert.That(result.Result).IsTypeOf<ForbidResult>();
    }

    private static TenantStorageSettingsController CreateController(
        IMediator mediator,
        Guid? userId = null,
        IResourceAssembler<TenantStorageSettingsDto, TenantStorageSettingsDto>? storageSettingsAssembler = null)
    {
        var userContext = Substitute.For<IUserContext>();
        userContext.UserId.Returns(userId ?? Guid.NewGuid());
        var services = new ServiceCollection()
            .AddSingleton(userContext)
            .BuildServiceProvider();

        return new TenantStorageSettingsController(
            mediator,
            storageSettingsAssembler ?? Substitute.For<IResourceAssembler<TenantStorageSettingsDto, TenantStorageSettingsDto>>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    RequestServices = services
                }
            }
        };
    }

    private static TenantStorageSettingsDto CreateSettings()
        => new()
        {
            Provider = StorageProviders.Local,
            MaxUploadBytes = 4096,
            TenantQuotaBytes = 8192,
            S3UploadUrlExpirationMinutes = 60
        };
}
