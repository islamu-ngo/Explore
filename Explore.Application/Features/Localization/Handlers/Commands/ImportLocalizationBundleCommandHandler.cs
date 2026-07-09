// ABOUTME: Handles admin static bundle imports through validated bundle writer persistence.
// ABOUTME: Invalidates runtime translation cache after a successful same-process bundle write.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Features.Localization.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain.Common.Localization;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Explore.Application.Features.Localization.Handlers.Commands;

public class ImportLocalizationBundleCommandHandler : IRequestHandler<ImportLocalizationBundleCommand, BaseCommandResponse<Guid>>
{
    private readonly IBundleFileWriter _bundleFileWriter;
    private readonly ITranslationResolver _translationResolver;
    private readonly ILogger<ImportLocalizationBundleCommandHandler> _logger;

    public ImportLocalizationBundleCommandHandler(
        IBundleFileWriter bundleFileWriter,
        ITranslationResolver translationResolver,
        ILogger<ImportLocalizationBundleCommandHandler> logger)
    {
        _bundleFileWriter = bundleFileWriter;
        _translationResolver = translationResolver;
        _logger = logger;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(
        ImportLocalizationBundleCommand request,
        CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        if (!CultureRegistry.TryGetEntry(request.Dto.LanguageCode, out var culture))
            throw new ValidationException([
                new ValidationFailure(nameof(request.Dto.LanguageCode), "Language code is not supported."),
            ]);

        if (request.Dto.Translations.Count == 0)
        {
            response.Success = false;
            response.Message = "Bundle import requires at least one translation.";
            response.Errors = [response.Message];
            return response;
        }

        try
        {
            var path = await _bundleFileWriter.WriteBundleAsync(
                culture.Code,
                request.Dto.Translations,
                cancellationToken);

            await _translationResolver.InvalidateLanguageAsync(culture.Code, cancellationToken);

            response.Success = true;
            response.Id = Guid.NewGuid();
            response.Message = $"Imported {request.Dto.Translations.Count} translations for language '{culture.Code}' → {path}";
            return response;
        }
        catch (BundleWriteException ex)
        {
            _logger.LogError(ex, "[LOCALIZATION] Static bundle import failed for {Language}", culture.Code);
            response.Success = false;
            response.Message = $"Failed to import bundle for '{culture.Code}': {ex.Message}";
            response.Errors = [response.Message];
            return response;
        }
    }
}
