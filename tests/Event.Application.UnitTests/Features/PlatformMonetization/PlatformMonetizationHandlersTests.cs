// ABOUTME: Tests instance-admin platform monetization query and revision update handlers.
// ABOUTME: Covers authorization ordering, validation, deterministic mapping, and serializable retries.

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.PlatformMonetization;
using Explore.Application.Exceptions;
using Explore.Application.Features.PlatformMonetization.Handlers.Commands;
using Explore.Application.Features.PlatformMonetization.Handlers.Queries;
using Explore.Application.Features.PlatformMonetization.Requests.Commands;
using Explore.Application.Features.PlatformMonetization.Requests.Queries;
using Explore.Domain;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.PlatformMonetization;

[Category("PlatformMonetization")]
public sealed class PlatformMonetizationHandlersTests
{
    private readonly IAdminContext _adminContext = Substitute.For<IAdminContext>();
    private readonly IPlatformFeePolicyRepository _feePolicies = Substitute.For<IPlatformFeePolicyRepository>();
    private readonly IPlatformContributionSettingRepository _contributions = Substitute.For<IPlatformContributionSettingRepository>();

    [Test]
    public async Task Query_WhenCallerIsNotInstanceAdmin_DeniesBeforeRepositories()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetPlatformMonetizationSettingsQueryHandler(_adminContext, _feePolicies, _contributions);

        await Assert.That(async () => await handler.Handle(new GetPlatformMonetizationSettingsQuery(), CancellationToken.None))
            .Throws<AuthorizationException>();
        await _feePolicies.DidNotReceive().GetActiveAsync(Arg.Any<CancellationToken>());
        await _contributions.DidNotReceive().GetActiveAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Query_WhenActiveRowsExist_MapsFlatDtoWithDeterministicChildren()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(true);
        PlatformFeePolicy fee = PlatformFeePolicy.CreateDefault().CreateRevision(
            true,
            250,
            [PlatformFeeFixedCharge.Create("USD", 25), PlatformFeeFixedCharge.Create("EUR", 20)]);
        PlatformContributionSetting contribution = PlatformContributionSetting.CreateInitial(
            true,
            "Support the platform",
            "Optional contribution",
            [
                PlatformContributionOption.Create(1_000, 2, false),
                PlatformContributionOption.Create(0, 0, true),
                PlatformContributionOption.Create(500, 1, false)
            ]);
        _feePolicies.GetActiveAsync(Arg.Any<CancellationToken>()).Returns(fee);
        _contributions.GetActiveAsync(Arg.Any<CancellationToken>()).Returns(contribution);
        var handler = new GetPlatformMonetizationSettingsQueryHandler(_adminContext, _feePolicies, _contributions);

        PlatformMonetizationSettingsDto result = await handler.Handle(new GetPlatformMonetizationSettingsQuery(), CancellationToken.None);

        await Assert.That(result.FeeBasisPoints).IsEqualTo(250);
        await Assert.That(result.FixedCharges.Select(charge => charge.CurrencyCode).SequenceEqual(["EUR", "USD"])).IsTrue();
        await Assert.That(result.FixedCharges.Select(charge => charge.AmountMinor).SequenceEqual([20L, 25L])).IsTrue();
        await Assert.That(result.ContributionOptions.Select(option => option.SortOrder).SequenceEqual([0, 1, 2])).IsTrue();
        await Assert.That(result.ContributionOptions.Select(option => option.ContributionBasisPoints).SequenceEqual([0, 500, 1_000])).IsTrue();
    }

    [Test]
    public async Task Update_WhenInvalid_ValidatesBeforeStartingTransaction()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(true);
        var unitOfWork = new RecordingUnitOfWork();
        var handler = CreateUpdateHandler(unitOfWork);

        await Assert.That(async () => await handler.Handle(CreateUpdate(feeBasisPoints: 10_001), CancellationToken.None))
            .Throws<ValidationException>();
        await Assert.That(unitOfWork.SerializableBoundaries).IsEqualTo(0);
        await _feePolicies.DidNotReceive().GetActiveAsync(Arg.Any<CancellationToken>());
        await _contributions.DidNotReceive().GetActiveAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Update_WhenCallerIsNotInstanceAdmin_DeniesBeforeValidationTransactionAndRepositories()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        var unitOfWork = new RecordingUnitOfWork();

        await Assert.That(async () => await CreateUpdateHandler(unitOfWork).Handle(CreateUpdate(feeBasisPoints: 10_001), CancellationToken.None))
            .Throws<AuthorizationException>();
        await Assert.That(unitOfWork.SerializableBoundaries).IsEqualTo(0);
        await _feePolicies.DidNotReceive().GetActiveAsync(Arg.Any<CancellationToken>());
        await _contributions.DidNotReceive().GetActiveAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Query_WhenActiveFeeIsMissing_ThrowsNotFound()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(true);
        _feePolicies.GetActiveAsync(Arg.Any<CancellationToken>()).Returns((PlatformFeePolicy?)null);
        _contributions.GetActiveAsync(Arg.Any<CancellationToken>()).Returns(CreateContribution());

        await Assert.That(async () => await new GetPlatformMonetizationSettingsQueryHandler(_adminContext, _feePolicies, _contributions)
                .Handle(new GetPlatformMonetizationSettingsQuery(), CancellationToken.None))
            .Throws<NotFoundException>();
    }

    [Test]
    public async Task Update_WhenVersionsMatch_UsesSerializableTransactionAndSavesRetirementsBeforeRevisions()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(true);
        PlatformFeePolicy fee = PlatformFeePolicy.CreateDefault();
        PlatformContributionSetting contribution = CreateContribution();
        _feePolicies.GetActiveAsync(Arg.Any<CancellationToken>()).Returns(fee);
        _contributions.GetActiveAsync(Arg.Any<CancellationToken>()).Returns(contribution);
        var unitOfWork = new RecordingUnitOfWork();
        var operations = new List<string>();
        _feePolicies.UpdateAsync(fee, Arg.Any<CancellationToken>()).Returns(_ => { operations.Add("fee-retired"); return Task.CompletedTask; });
        _contributions.UpdateAsync(contribution, Arg.Any<CancellationToken>()).Returns(_ => { operations.Add("contribution-retired"); return Task.CompletedTask; });
        _feePolicies.AddAsync(Arg.Any<PlatformFeePolicy>(), Arg.Any<CancellationToken>()).Returns(_ => { operations.Add("fee-revision"); return Task.CompletedTask; });
        _contributions.AddAsync(Arg.Any<PlatformContributionSetting>(), Arg.Any<CancellationToken>()).Returns(_ => { operations.Add("contribution-revision"); return Task.CompletedTask; });

        var result = await CreateUpdateHandler(unitOfWork).Handle(CreateUpdate(), CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(unitOfWork.SerializableBoundaries).IsEqualTo(1);
        await Assert.That(operations.SequenceEqual(["fee-retired", "contribution-retired", "fee-revision", "contribution-revision"])).IsTrue();
        await _feePolicies.Received(1).AddAsync(Arg.Is<PlatformFeePolicy>(policy => policy.VersionNumber == 2 && policy.IsActive), Arg.Any<CancellationToken>());
        await _contributions.Received(1).AddAsync(Arg.Is<PlatformContributionSetting>(setting => setting.VersionNumber == 2 && setting.IsActive), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Update_WhenTransactionRetries_ReloadsBothActiveRowsForEachAttempt()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(true);
        _feePolicies.GetActiveAsync(Arg.Any<CancellationToken>()).Returns(PlatformFeePolicy.CreateDefault(), PlatformFeePolicy.CreateDefault());
        _contributions.GetActiveAsync(Arg.Any<CancellationToken>()).Returns(CreateContribution(), CreateContribution());
        var unitOfWork = new RecordingUnitOfWork(attempts: 2);

        var result = await CreateUpdateHandler(unitOfWork).Handle(CreateUpdate(), CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(unitOfWork.DelegateAttempts).IsEqualTo(2);
        await _feePolicies.Received(2).GetActiveAsync(Arg.Any<CancellationToken>());
        await _contributions.Received(2).GetActiveAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Update_WhenExpectedVersionIsStale_ThrowsConflictWithoutWrites()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(true);
        _feePolicies.GetActiveAsync(Arg.Any<CancellationToken>()).Returns(PlatformFeePolicy.CreateDefault());
        _contributions.GetActiveAsync(Arg.Any<CancellationToken>()).Returns(CreateContribution());
        var unitOfWork = new RecordingUnitOfWork();

        await Assert.That(async () => await CreateUpdateHandler(unitOfWork).Handle(CreateUpdate(expectedFeeVersion: 99), CancellationToken.None))
            .Throws<ConcurrencyConflictException>();
        await _feePolicies.DidNotReceive().UpdateAsync(Arg.Any<PlatformFeePolicy>(), Arg.Any<CancellationToken>());
        await _contributions.DidNotReceive().UpdateAsync(Arg.Any<PlatformContributionSetting>(), Arg.Any<CancellationToken>());
        await _feePolicies.DidNotReceive().AddAsync(Arg.Any<PlatformFeePolicy>(), Arg.Any<CancellationToken>());
        await _contributions.DidNotReceive().AddAsync(Arg.Any<PlatformContributionSetting>(), Arg.Any<CancellationToken>());
    }

    private UpdatePlatformMonetizationSettingsCommandHandler CreateUpdateHandler(IUnitOfWork unitOfWork) =>
        new(_adminContext, _feePolicies, _contributions, unitOfWork);

    private static UpdatePlatformMonetizationSettingsCommand CreateUpdate(int feeBasisPoints = 250, int expectedFeeVersion = 1) => new()
    {
        Settings = new UpdatePlatformMonetizationSettingsDto
        {
            FeeEnabled = true,
            FeeBasisPoints = feeBasisPoints,
            FixedCharges = [new PlatformFeeFixedChargeDto { CurrencyCode = "USD", AmountMinor = 25 }],
            ExpectedFeeVersion = expectedFeeVersion,
            ContributionEnabled = true,
            ContributionHeading = "Support the platform",
            ContributionBody = "Optional contribution",
            ContributionOptions =
            [
                new PlatformContributionOptionDto { ContributionBasisPoints = 0, SortOrder = 0, IsDefault = true },
                new PlatformContributionOptionDto { ContributionBasisPoints = 500, SortOrder = 1, IsDefault = false }
            ],
            ExpectedContributionVersion = 1
        }
    };

    private static PlatformContributionSetting CreateContribution() => PlatformContributionSetting.CreateInitial(
        false,
        string.Empty,
        string.Empty,
        [PlatformContributionOption.Create(0, 0, true)]);

    private sealed class RecordingUnitOfWork(int attempts = 1) : IUnitOfWork
    {
        public int SerializableBoundaries { get; private set; }
        public int DelegateAttempts { get; private set; }

        public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken ct = default) => operation(ct);

        public Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default) => operation(ct);

        public async Task<T> ExecuteSerializableAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default)
        {
            SerializableBoundaries++;
            T result = default!;
            for (var attempt = 0; attempt < attempts; attempt++)
            {
                DelegateAttempts++;
                result = await operation(ct);
            }

            return result;
        }
    }
}
