// ABOUTME: Saves the non-secret instance onboarding profile settings.
// ABOUTME: Validates manually and writes only the established system settings in one transaction.

using System.Linq;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Onboarding.Validators;
using Explore.Application.Features.InstanceOnboarding.Common;
using Explore.Application.Features.InstanceOnboarding.Requests.Commands;
using Explore.Application.Onboarding;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.InstanceOnboarding.Handlers.Commands;

public sealed class SaveInstanceOnboardingProfileCommandHandler(
    IInstanceBootstrapStateRepository instanceBootstrapStateRepository,
    ISystemSettingRepository systemSettingRepository,
    ISetupSecretProvider setupSecretProvider,
    IInstanceBootstrapAuditLogger instanceBootstrapAuditLogger,
    IUnitOfWork unitOfWork)
    : IRequestHandler<SaveInstanceOnboardingProfileCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(
        SaveInstanceOnboardingProfileCommand request,
        CancellationToken cancellationToken)
    {
        var bootstrap = await instanceBootstrapStateRepository.GetCurrent(cancellationToken);
        if (bootstrap?.IsCompleted == true || !setupSecretProvider.IsSetupModeActive)
        {
            return new BaseCommandResponse<Guid>
            {
                Id = bootstrap?.Id ?? Guid.Empty,
                Success = false,
                Message = "Setup mode is no longer active."
            };
        }

        var validator = new SelfHostOnboardingProfileDtoValidator();
        var validation = await validator.ValidateAsync(request.Profile, cancellationToken);
        if (!validation.IsValid)
        {
            return new BaseCommandResponse<Guid>
            {
                Success = false,
                Message = "Invalid onboarding profile.",
                Errors = validation.Errors.Select(error => error.ErrorMessage).ToList()
            };
        }

        var profile = InstanceOnboardingProfileSettingHelpers.Normalize(request.Profile);
        await unitOfWork.ExecuteInTransactionAsync(
            async transactionToken => await InstanceOnboardingProfileSettingHelpers.PersistAsync(
                systemSettingRepository,
                profile,
                transactionToken),
            cancellationToken);

        instanceBootstrapAuditLogger.Log(new InstanceBootstrapAuditEvent(
            InstanceBootstrapAuditEventType.SetupProfileSaved,
            Operation: "instance_onboarding_profile_save",
            Outcome: "saved"));

        return new BaseCommandResponse<Guid>
        {
            Id = bootstrap?.Id ?? Guid.Empty,
            Success = true,
            Message = "Instance onboarding profile saved successfully."
        };
    }
}
