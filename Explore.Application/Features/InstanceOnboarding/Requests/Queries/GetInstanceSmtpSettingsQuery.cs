// ABOUTME: MediatR query request for fetching the instance SMTP settings.
// ABOUTME: Returns InstanceSmtpSettingsDto.
using Explore.Application.DTOs.Onboarding;
using MediatR;

namespace Explore.Application.Features.InstanceOnboarding.Requests.Queries;

public class GetInstanceSmtpSettingsQuery : IRequest<InstanceSmtpSettingsDto>
{
}
