// ABOUTME: API controller tests for instance storage admin operations.
// ABOUTME: Verifies provider test and usage recalculation routes remain instance-admin gated.

using Explore.API.Controllers;
using Explore.API.Hateoas;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Onboarding;
using Explore.Application.Features.InstanceOnboarding.Requests.Commands;
using Explore.Application.Features.InstanceOnboarding.Requests.Queries;
using Explore.Application.Hateoas;
using Explore.Domain;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Event.Api.IntegrationTests.Features;

public sealed class InstanceStorageAdminControllerTests
{
    [Test]
    public async Task GetStorageSettings_WhenInstanceAdmin_ReturnsHalResource()
    {
        var mediator = Substitute.For<IMediator>();
        var adminContext = Substitute.For<IAdminContext>();
        var assembler = Substitute.For<IResourceAssembler<InstanceStorageSettingsDto, InstanceStorageSettingsDto>>();
        var settings = new InstanceStorageSettingsDto
        {
            Provider = StorageProviders.Local,
            DefaultMaxUploadBytes = 4096,
            DefaultTenantQuotaBytes = 8192
        };
        var halResource = new HalResource<InstanceStorageSettingsDto>(settings);
        adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(true);
        mediator.Send(Arg.Any<GetInstanceStorageSettingsQuery>(), Arg.Any<CancellationToken>())
            .Returns(settings);
        assembler.ToResource(settings, Arg.Any<HttpContext>()).Returns(halResource);
        var controller = CreateController(mediator, adminContext, storageSettingsAssembler: assembler);

        var result = await controller.GetStorageSettings(CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        await Assert.That(ok).IsNotNull();
        await Assert.That(ok!.Value).IsEqualTo(halResource);
        await assembler.Received(1).ToResource(settings, Arg.Any<HttpContext>());
    }

    [Test]
    public async Task TestStorageConnection_WhenInstanceAdmin_ReturnsProviderStatus()
    {
        var mediator = Substitute.For<IMediator>();
        var adminContext = Substitute.For<IAdminContext>();
        adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(true);
        mediator.Send(Arg.Any<TestInstanceStorageProviderQuery>(), Arg.Any<CancellationToken>())
            .Returns(new InstanceStorageProviderStatusDto
            {
                Provider = StorageProviders.Local,
                IsAvailable = true,
                SupportsServerSideStreaming = true,
                SupportsBrowserDirectUpload = false
            });
        var controller = CreateController(mediator, adminContext);

        var result = await controller.TestStorageConnection(CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        await Assert.That(ok).IsNotNull();
        var status = ok!.Value as InstanceStorageProviderStatusDto;
        await Assert.That(status).IsNotNull();
        await Assert.That(status!.Provider).IsEqualTo(StorageProviders.Local);
        await Assert.That(status.IsAvailable).IsTrue();
        await mediator.Received(1).Send(Arg.Any<TestInstanceStorageProviderQuery>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task TestStorageConnection_WhenNotInstanceAdmin_ReturnsForbidden()
    {
        var mediator = Substitute.For<IMediator>();
        var adminContext = Substitute.For<IAdminContext>();
        adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        var setupSecretProvider = Substitute.For<ISetupSecretProvider>();
        setupSecretProvider.IsSetupModeActive.Returns(false);
        var controller = CreateController(mediator, adminContext, setupSecretProvider);

        var result = await controller.TestStorageConnection(CancellationToken.None);

        await Assert.That(result.Result).IsTypeOf<ForbidResult>();
        await mediator.DidNotReceive().Send(Arg.Any<TestInstanceStorageProviderQuery>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RecalculateStorageUsage_WhenInstanceAdmin_ReturnsUsageSummary()
    {
        var mediator = Substitute.For<IMediator>();
        var adminContext = Substitute.For<IAdminContext>();
        adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(true);
        mediator.Send(Arg.Any<RecalculateInstanceStorageUsageCommand>(), Arg.Any<CancellationToken>())
            .Returns(new InstanceStorageUsageDto
            {
                UsedBytes = 4096,
                ReservedBytes = 1024,
                ObjectCount = 2
            });
        var controller = CreateController(mediator, adminContext);

        var result = await controller.RecalculateStorageUsage(CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        await Assert.That(ok).IsNotNull();
        var usage = ok!.Value as InstanceStorageUsageDto;
        await Assert.That(usage).IsNotNull();
        await Assert.That(usage!.UsedBytes).IsEqualTo(4096);
        await Assert.That(usage.ReservedBytes).IsEqualTo(1024);
        await Assert.That(usage.ObjectCount).IsEqualTo(2);
        await mediator.Received(1).Send(Arg.Any<RecalculateInstanceStorageUsageCommand>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RecalculateStorageUsage_WhenNotInstanceAdmin_ReturnsForbidden()
    {
        var mediator = Substitute.For<IMediator>();
        var adminContext = Substitute.For<IAdminContext>();
        adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        var setupSecretProvider = Substitute.For<ISetupSecretProvider>();
        setupSecretProvider.IsSetupModeActive.Returns(false);
        var controller = CreateController(mediator, adminContext, setupSecretProvider);

        var result = await controller.RecalculateStorageUsage(CancellationToken.None);

        await Assert.That(result.Result).IsTypeOf<ForbidResult>();
        await mediator.DidNotReceive().Send(Arg.Any<RecalculateInstanceStorageUsageCommand>(), Arg.Any<CancellationToken>());
    }

    private static InstanceSettingsController CreateController(
        IMediator mediator,
        IAdminContext adminContext,
        ISetupSecretProvider? setupSecretProvider = null,
        IResourceAssembler<InstanceStorageSettingsDto, InstanceStorageSettingsDto>? storageSettingsAssembler = null)
    {
        return new InstanceSettingsController(
            mediator,
            adminContext,
            setupSecretProvider ?? Substitute.For<ISetupSecretProvider>(),
            Substitute.For<IDeploymentModeProvider>(),
            storageSettingsAssembler ?? Substitute.For<IResourceAssembler<InstanceStorageSettingsDto, InstanceStorageSettingsDto>>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }
}
