// ABOUTME: Command wrapping TMS API-key rotation for localization provider authentication.
// ABOUTME: Stores only protected secret metadata and returns a redacted command response.

using Explore.Application.DTOs.Localization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Localization.Requests.Commands;

public sealed class RotateLocalizationTmsApiKeyCommand : IRequest<BaseCommandResponse<Guid>>
{
    public RotateLocalizationTmsApiKeyDto Dto { get; set; } = new();
}
