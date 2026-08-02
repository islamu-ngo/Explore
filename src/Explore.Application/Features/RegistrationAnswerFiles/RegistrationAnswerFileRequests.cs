// ABOUTME: Defines tenant-scoped administrative query and manual-release command contracts.
// ABOUTME: Keeps release authorization identity, validation, and immutable persistence in the Application layer.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Registration;
using Explore.Application.Responses;
using Explore.Domain;
using FluentValidation;
using MediatR;

namespace Explore.Application.Features.RegistrationAnswerFiles;

public sealed record GetRegistrationAnswerFileQuery(Guid TenantId, Guid Id)
    : IRequest<RegistrationAnswerFileDto?>;

public sealed record ReleaseRegistrationAnswerFileCommand(Guid TenantId, Guid Id, string Reason)
    : IRequest<BaseCommandResponse<Guid>>;

public sealed class ReleaseRegistrationAnswerFileCommandValidator
    : AbstractValidator<ReleaseRegistrationAnswerFileCommand>
{
    public ReleaseRegistrationAnswerFileCommandValidator()
    {
        RuleFor(command => command.TenantId).NotEmpty();
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.Reason).NotEmpty().MaximumLength(500);
    }
}

public sealed class GetRegistrationAnswerFileQueryHandler(IRegistrationAnswerFileRepository repository)
    : IRequestHandler<GetRegistrationAnswerFileQuery, RegistrationAnswerFileDto?>
{
    public async Task<RegistrationAnswerFileDto?> Handle(
        GetRegistrationAnswerFileQuery request,
        CancellationToken cancellationToken)
    {
        RegistrationAnswerFile? file = await repository.GetAsync(request.TenantId, request.Id, cancellationToken);
        if (file is null)
        {
            return null;
        }

        RegistrationAnswerFileRelease? release = file.IsReleased
            ? await repository.GetReleaseAsync(request.TenantId, request.Id, cancellationToken)
            : null;
        return Map(file, release);
    }

    private static RegistrationAnswerFileDto Map(
        RegistrationAnswerFile file,
        RegistrationAnswerFileRelease? release)
        => new(
            file.Id,
            file.RegistrationSubmissionId,
            file.RegistrationFormFieldId,
            file.StorageObjectId,
            file.SafeDisplayName,
            file.ContentType,
            file.Extension,
            file.Size,
            file.QuarantineState,
            file.ScanStatus,
            file.QuarantinedAt,
            file.ReleasedBy,
            file.ReleasedAt,
            release?.Reason);
}

public sealed class ReleaseRegistrationAnswerFileCommandHandler(
    IRegistrationAnswerFileRepository repository,
    ICurrentUserService currentUserService,
    TimeProvider timeProvider)
    : IRequestHandler<ReleaseRegistrationAnswerFileCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(
        ReleaseRegistrationAnswerFileCommand request,
        CancellationToken cancellationToken)
    {
        await new ReleaseRegistrationAnswerFileCommandValidator().ValidateAndThrowAsync(request, cancellationToken);
        Guid? actorId = currentUserService.UserId;
        if (!currentUserService.IsAuthenticated || actorId is null || actorId == Guid.Empty)
        {
            return Failure(request.Id, "Authenticated release operator could not be resolved.",
                "registration_answer_file_release_operator_required");
        }

        RegistrationAnswerFileReleaseResult? result = await repository.ReleaseAsync(
            request.TenantId,
            request.Id,
            actorId.Value,
            request.Reason,
            timeProvider.GetUtcNow().UtcDateTime,
            cancellationToken);
        if (result is null)
        {
            return Failure(request.Id, "Registration answer file was not found.",
                "registration_answer_file_not_found");
        }

        return new BaseCommandResponse<Guid>
        {
            Id = result.File.Id,
            Success = true,
            Message = result.WasAlreadyReleased
                ? "Registration answer file was already released; the original audit was preserved."
                : "Registration answer file released."
        };
    }

    private static BaseCommandResponse<Guid> Failure(Guid id, string message, string code)
        => new()
        {
            Id = id,
            Success = false,
            Message = message,
            FailureCode = code,
            Errors = [message]
        };
}
