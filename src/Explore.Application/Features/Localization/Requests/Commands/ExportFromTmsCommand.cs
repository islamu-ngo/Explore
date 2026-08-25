// ABOUTME: MediatR command to pull translations from the configured TMS provider and refresh cache.
// ABOUTME: Triggers an export of all translations for the specified language from TMS.

using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Localization.Requests.Commands;

public sealed record ExportFromTmsCommand : IRequest<BaseCommandResponse<Guid>>
{
    public required string LanguageCode { get; init; }
}
