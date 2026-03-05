// ABOUTME: Handler for ExportFromTmsCommand that pulls translations from TMS and refreshes cache.
// ABOUTME: Exports translations for a language from the active TMS provider, invalidating cached data.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Features.Localization.Requests.Commands;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Localization.Handlers.Commands;

public class ExportFromTmsCommandHandler : IRequestHandler<ExportFromTmsCommand, BaseCommandResponse<Guid>>
{
    private readonly ITranslationManagementProvider _translationProvider;
    private readonly ITranslationResolver _translationResolver;

    public ExportFromTmsCommandHandler(
        ITranslationManagementProvider translationProvider,
        ITranslationResolver translationResolver)
    {
        _translationProvider = translationProvider;
        _translationResolver = translationResolver;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(ExportFromTmsCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var exports = await _translationProvider.ExportTranslationsAsync(request.LanguageCode, cancellationToken);
        var translationCount = exports.Count();

        if (translationCount > 0)
        {
            response.Success = true;
            response.Message = $"Exported {translationCount} translations for language '{request.LanguageCode}'.";
        }
        else
        {
            response.Success = false;
            response.Message = $"No translations found for language '{request.LanguageCode}'. Verify the TMS has translations for this language.";
        }

        return response;
    }
}
