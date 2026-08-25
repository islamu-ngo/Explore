// ABOUTME: MediatR command for writing an admin-supplied static localization bundle.
// ABOUTME: Keeps direct bundle imports in Application while Infrastructure owns disk persistence.

using Explore.Application.DTOs.Localization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Localization.Requests.Commands;

public sealed record ImportLocalizationBundleCommand : IRequest<BaseCommandResponse<Guid>>
{
    public required ImportLocalizationBundleDto Dto { get; init; }
}
