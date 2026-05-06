// ABOUTME: Tests lookup-backed event registration update DTO validation rules.
// ABOUTME: Keeps update command validation failures deterministic before persistence.

using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventRegistration;
using Explore.Application.DTOs.EventRegistration.Validators;
using NSubstitute;

namespace Event.Application.UnitTests.DTOs.EventRegistration.Validators;

public sealed class UpdateEventRegistrationDtoValidatorTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IEventSessionRepository _eventSessionRepository = Substitute.For<IEventSessionRepository>();
    private readonly IApprovalStatusRepository _approvalStatusRepository = Substitute.For<IApprovalStatusRepository>();
    private readonly UpdateEventRegistrationDtoValidator _validator;

    public UpdateEventRegistrationDtoValidatorTests()
    {
        _validator = new UpdateEventRegistrationDtoValidator(
            _userRepository,
            _eventSessionRepository,
            _approvalStatusRepository);
    }

    [Test]
    public async Task Validate_WithValidLookups_ReturnsValid()
    {
        var dto = CreateValidDto();
        SetupValidLookups(dto);

        var result = await _validator.ValidateAsync(dto);

        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task Validate_WithMissingUser_ReturnsUserError()
    {
        var dto = CreateValidDto();
        _userRepository.Exists(dto.UserId).Returns(false);
        _eventSessionRepository.Exists(dto.EventSessionId).Returns(true);
        _approvalStatusRepository.Exists(dto.ApprovalStatusId!.Value).Returns(true);

        var result = await _validator.ValidateAsync(dto);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Select(error => error.ErrorMessage)).Contains("User Id not found");
    }

    [Test]
    public async Task Validate_WithMissingEventSession_ReturnsSessionError()
    {
        var dto = CreateValidDto();
        _userRepository.Exists(dto.UserId).Returns(true);
        _eventSessionRepository.Exists(dto.EventSessionId).Returns(false);
        _approvalStatusRepository.Exists(dto.ApprovalStatusId!.Value).Returns(true);

        var result = await _validator.ValidateAsync(dto);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Select(error => error.ErrorMessage)).Contains("Event Session Id not found");
    }

    [Test]
    public async Task Validate_WithNullApprovalStatus_DoesNotQueryApprovalStatuses()
    {
        var dto = CreateValidDto();
        dto.ApprovalStatusId = null;
        _userRepository.Exists(dto.UserId).Returns(true);
        _eventSessionRepository.Exists(dto.EventSessionId).Returns(true);

        var result = await _validator.ValidateAsync(dto);

        await Assert.That(result.IsValid).IsTrue();
        await _approvalStatusRepository.DidNotReceive().Exists(Arg.Any<int>());
    }

    private void SetupValidLookups(UpdateEventRegistrationDto dto)
    {
        _userRepository.Exists(dto.UserId).Returns(true);
        _eventSessionRepository.Exists(dto.EventSessionId).Returns(true);
        _approvalStatusRepository.Exists(dto.ApprovalStatusId!.Value).Returns(true);
    }

    private static UpdateEventRegistrationDto CreateValidDto() => new()
    {
        Id = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        EventSessionId = Guid.NewGuid(),
        ApprovalStatusId = 1,
        TenantId = Guid.NewGuid()
    };
}
