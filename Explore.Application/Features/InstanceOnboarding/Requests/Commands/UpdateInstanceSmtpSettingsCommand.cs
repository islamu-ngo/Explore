// ABOUTME: MediatR command for updating instance SMTP settings.
// ABOUTME: Carries the UpdateInstanceSmtpSettingsDto payload.
using Explore.Application.DTOs.Onboarding;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.InstanceOnboarding.Requests.Commands;

public class UpdateInstanceSmtpSettingsCommand : IRequest<BaseCommandResponse<Guid>>
{
    public Guid UserId { get; set; }
    public required InstanceSmtpSettingsDto Settings { get; set; } = new();
}
