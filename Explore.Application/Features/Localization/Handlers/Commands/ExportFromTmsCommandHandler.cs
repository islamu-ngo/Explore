// ABOUTME: Handler for ExportFromTmsCommand — pulls translations from TMS, writes bundle to disk, invalidates cache.
// ABOUTME: The persistence seam is IBundleFileWriter so a future DistributedBundleFileWriter can replace local-disk.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Features.Localization.Requests.Commands;
using Explore.Application.Responses;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Explore.Application.Features.Localization.Handlers.Commands;

public class ExportFromTmsCommandHandler : IRequestHandler<ExportFromTmsCommand, BaseCommandResponse<Guid>>
{
    private readonly ITranslationManagementProvider _translationProvider;
    private readonly ITranslationResolver _translationResolver;
    private readonly IBundleFileWriter _bundleFileWriter;
    private readonly ILogger<ExportFromTmsCommandHandler> _logger;

    public ExportFromTmsCommandHandler(
        ITranslationManagementProvider translationProvider,
        ITranslationResolver translationResolver,
        IBundleFileWriter bundleFileWriter,
        ILogger<ExportFromTmsCommandHandler> logger)
    {
        _translationProvider = translationProvider;
        _translationResolver = translationResolver;
        _bundleFileWriter = bundleFileWriter;
        _logger = logger;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(ExportFromTmsCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        IEnumerable<TranslationExport> exports;
        try
        {
            exports = await _translationProvider.ExportTranslationsAsync(request.LanguageCode, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LOCALIZATION] Export from TMS failed for {Language}", request.LanguageCode);
            response.Success = false;
            response.Message = $"Export from TMS failed for language '{request.LanguageCode}': {ex.Message}";
            return response;
        }

        var translations = exports
            .Where(e => !string.IsNullOrEmpty(e.KeyName))
            .GroupBy(e => e.KeyName)
            .ToDictionary(g => g.Key, g => g.First().Value ?? string.Empty);

        if (translations.Count == 0)
        {
            response.Success = false;
            response.Message =
                $"No translations found for language '{request.LanguageCode}'. Verify the TMS has translations for this language.";
            return response;
        }

        try
        {
            var path = await _bundleFileWriter.WriteBundleAsync(request.LanguageCode, translations, cancellationToken);
            await _translationResolver.InvalidateLanguageAsync(request.LanguageCode, cancellationToken);

            response.Success = true;
            response.Id = Guid.NewGuid();
            response.Message =
                $"Exported {translations.Count} translations for language '{request.LanguageCode}' → {path}";
            return response;
        }
        catch (BundleWriteException ex)
        {
            _logger.LogError(ex, "[LOCALIZATION] Bundle persistence failed for {Language}", request.LanguageCode);
            response.Success = false;
            response.Message = $"Failed to persist bundle for '{request.LanguageCode}': {ex.Message}";
            return response;
        }
    }
}
