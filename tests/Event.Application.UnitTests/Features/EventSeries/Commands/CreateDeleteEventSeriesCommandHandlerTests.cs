// ABOUTME: Unit tests for EventSeries create/delete tenant-admin containment.
// ABOUTME: Proves current-tenant admins can mutate series while other callers fail before repository writes.

using AutoMapper;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventSeries;
using Explore.Application.Exceptions;
using Explore.Application.Features.EventSeries.Handlers.Commands;
using Explore.Application.Features.EventSeries.Requests.Commands;
using Explore.Domain;
using NSubstitute;
using DomainEventSeries = Explore.Domain.EventSeries;

namespace Event.Application.UnitTests.Features.EventSeries.Commands;

public sealed class CreateDeleteEventSeriesCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.CreateVersion7();
    private static readonly Guid OtherTenantId = Guid.CreateVersion7();
    private static readonly Guid UserId = Guid.CreateVersion7();

    private readonly IEventSeriesRepository _seriesRepository = Substitute.For<IEventSeriesRepository>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly IAdminContext _adminContext = Substitute.For<IAdminContext>();
    private readonly IStorageObjectRepository _storageObjectRepository = Substitute.For<IStorageObjectRepository>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();

    public CreateDeleteEventSeriesCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(TenantId);
        _adminContext.ResolveUserIdAsync(Arg.Any<CancellationToken>()).Returns(UserId);
    }

    [Test]
    public async Task Create_WhenCurrentTenantAdmin_CreatesSeriesInAmbientTenant()
    {
        _adminContext.GetAdminTenantIdsAsync(UserId, Arg.Any<CancellationToken>()).Returns([TenantId]);
        var mappedSeries = CreateSeries(Guid.Empty);
        _mapper.Map<DomainEventSeries>(Arg.Any<CreateEventSeriesDto>()).Returns(mappedSeries);
        _seriesRepository.Create(mappedSeries).Returns(mappedSeries);

        var result = await CreateHandler().Handle(new CreateEventSeriesCommand
        {
            EventSeriesDto = CreateDto()
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(mappedSeries.TenantId).IsEqualTo(TenantId);
        await _adminContext.Received(1).GetAdminTenantIdsAsync(UserId, Arg.Any<CancellationToken>());
        await _adminContext.DidNotReceive().IsTenantAdminAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _seriesRepository.Received(1).Create(mappedSeries);
    }

    [Test]
    public async Task Create_WhenRegularUser_DeniesBeforeRepositoryCalls()
    {
        _adminContext.GetAdminTenantIdsAsync(UserId, Arg.Any<CancellationToken>()).Returns([]);

        await Assert.ThrowsAsync<AuthorizationException>(() => CreateHandler().Handle(new CreateEventSeriesCommand
        {
            EventSeriesDto = CreateDto()
        }, CancellationToken.None));

        await _seriesRepository.DidNotReceive().Create(Arg.Any<DomainEventSeries>());
        await _storageObjectRepository.DidNotReceive().GetById(Arg.Any<Guid>());
    }

    [Test]
    public async Task Create_WhenAdminOnlyInOtherTenant_DeniesBeforeRepositoryCalls()
    {
        _adminContext.GetAdminTenantIdsAsync(UserId, Arg.Any<CancellationToken>()).Returns([OtherTenantId]);

        await Assert.ThrowsAsync<AuthorizationException>(() => CreateHandler().Handle(new CreateEventSeriesCommand
        {
            EventSeriesDto = CreateDto()
        }, CancellationToken.None));

        await _adminContext.Received(1).GetAdminTenantIdsAsync(UserId, Arg.Any<CancellationToken>());
        await _adminContext.DidNotReceive().IsTenantAdminAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _adminContext.DidNotReceive().IsInstanceAdminAsync(Arg.Any<CancellationToken>());
        await _seriesRepository.DidNotReceive().Create(Arg.Any<DomainEventSeries>());
    }

    [Test]
    public async Task Create_WhenInstanceAdminButNotCurrentTenantAdmin_DeniesWithoutShortcut()
    {
        _adminContext.GetAdminTenantIdsAsync(UserId, Arg.Any<CancellationToken>()).Returns([]);
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(true);

        await Assert.ThrowsAsync<AuthorizationException>(() => CreateHandler().Handle(new CreateEventSeriesCommand
        {
            EventSeriesDto = CreateDto()
        }, CancellationToken.None));

        await _adminContext.DidNotReceive().IsInstanceAdminAsync(Arg.Any<CancellationToken>());
        await _adminContext.DidNotReceive().IsTenantAdminAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _seriesRepository.DidNotReceive().Create(Arg.Any<DomainEventSeries>());
    }

    [Test]
    public async Task Create_WhenExplicitTenantAdminAndInstanceAdmin_AllowsByDirectMembership()
    {
        _adminContext.GetAdminTenantIdsAsync(UserId, Arg.Any<CancellationToken>()).Returns([TenantId]);
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(true);
        var mappedSeries = CreateSeries(Guid.Empty);
        _mapper.Map<DomainEventSeries>(Arg.Any<CreateEventSeriesDto>()).Returns(mappedSeries);
        _seriesRepository.Create(mappedSeries).Returns(mappedSeries);

        var result = await CreateHandler().Handle(new CreateEventSeriesCommand
        {
            EventSeriesDto = CreateDto()
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _adminContext.DidNotReceive().IsInstanceAdminAsync(Arg.Any<CancellationToken>());
        await _seriesRepository.Received(1).Create(mappedSeries);
    }

    [Test]
    public async Task Delete_WhenCurrentTenantAdmin_DeletesVisibleSeries()
    {
        var series = CreateSeries(TenantId);
        _adminContext.GetAdminTenantIdsAsync(UserId, Arg.Any<CancellationToken>()).Returns([TenantId]);
        _seriesRepository.GetById(series.Id).Returns(series);

        var result = await DeleteHandler().Handle(new DeleteEventSeriesCommand { Id = series.Id }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _adminContext.DidNotReceive().IsTenantAdminAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _seriesRepository.Received(1).Delete(series);
    }

    [Test]
    public async Task Delete_WhenRegularUser_DeniesBeforeRepositoryCalls()
    {
        _adminContext.GetAdminTenantIdsAsync(UserId, Arg.Any<CancellationToken>()).Returns([]);

        await Assert.ThrowsAsync<AuthorizationException>(() => DeleteHandler().Handle(
            new DeleteEventSeriesCommand { Id = Guid.CreateVersion7() },
            CancellationToken.None));

        await _seriesRepository.DidNotReceive().GetById(Arg.Any<Guid>());
        await _seriesRepository.DidNotReceive().Delete(Arg.Any<DomainEventSeries>());
    }

    [Test]
    public async Task Delete_WhenAdminOnlyInOtherTenant_DeniesBeforeRepositoryCalls()
    {
        _adminContext.GetAdminTenantIdsAsync(UserId, Arg.Any<CancellationToken>()).Returns([OtherTenantId]);

        await Assert.ThrowsAsync<AuthorizationException>(() => DeleteHandler().Handle(
            new DeleteEventSeriesCommand { Id = Guid.CreateVersion7() },
            CancellationToken.None));

        await _adminContext.Received(1).GetAdminTenantIdsAsync(UserId, Arg.Any<CancellationToken>());
        await _adminContext.DidNotReceive().IsTenantAdminAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _adminContext.DidNotReceive().IsInstanceAdminAsync(Arg.Any<CancellationToken>());
        await _seriesRepository.DidNotReceive().GetById(Arg.Any<Guid>());
    }

    [Test]
    public async Task Delete_WhenInstanceAdminButNotCurrentTenantAdmin_DeniesWithoutShortcut()
    {
        _adminContext.GetAdminTenantIdsAsync(UserId, Arg.Any<CancellationToken>()).Returns([]);
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(true);

        await Assert.ThrowsAsync<AuthorizationException>(() => DeleteHandler().Handle(
            new DeleteEventSeriesCommand { Id = Guid.CreateVersion7() },
            CancellationToken.None));

        await _adminContext.DidNotReceive().IsInstanceAdminAsync(Arg.Any<CancellationToken>());
        await _adminContext.DidNotReceive().IsTenantAdminAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _seriesRepository.DidNotReceive().GetById(Arg.Any<Guid>());
    }

    [Test]
    public async Task Delete_WhenExplicitTenantAdminAndInstanceAdmin_AllowsByDirectMembership()
    {
        var series = CreateSeries(TenantId);
        _adminContext.GetAdminTenantIdsAsync(UserId, Arg.Any<CancellationToken>()).Returns([TenantId]);
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(true);
        _seriesRepository.GetById(series.Id).Returns(series);

        var result = await DeleteHandler().Handle(new DeleteEventSeriesCommand { Id = series.Id }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _adminContext.DidNotReceive().IsInstanceAdminAsync(Arg.Any<CancellationToken>());
        await _seriesRepository.Received(1).Delete(series);
    }

    [Test]
    public async Task Delete_WhenCurrentTenantAdminAndSeriesInvisible_ReturnsNotFoundWithoutDelete()
    {
        var seriesId = Guid.CreateVersion7();
        _adminContext.GetAdminTenantIdsAsync(UserId, Arg.Any<CancellationToken>()).Returns([TenantId]);
        _seriesRepository.GetById(seriesId).Returns((DomainEventSeries?)null);

        var result = await DeleteHandler().Handle(new DeleteEventSeriesCommand { Id = seriesId }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).IsEqualTo("Event series not found.");
        await _seriesRepository.DidNotReceive().Delete(Arg.Any<DomainEventSeries>());
    }

    private CreateEventSeriesCommandHandler CreateHandler() => new(
        _seriesRepository,
        _tenantContext,
        _adminContext,
        _storageObjectRepository,
        _mapper);

    private DeleteEventSeriesCommandHandler DeleteHandler() => new(
        _seriesRepository,
        _tenantContext,
        _adminContext);

    private static CreateEventSeriesDto CreateDto() => new()
    {
        Title = "Community series",
        ActorId = Guid.CreateVersion7()
    };

    private static DomainEventSeries CreateSeries(Guid tenantId) => new()
    {
        Id = Guid.CreateVersion7(),
        TenantId = tenantId,
        Title = "Community series",
        ActorId = Guid.CreateVersion7(),
        VisibilityTypeId = 1,
        VisibilityType = new VisibilityType
        {
            Id = 1,
            MasterCode = "PUBLIC",
            FullName = "Public"
        },
        Tenant = null!,
        Actor = null!
    };
}
