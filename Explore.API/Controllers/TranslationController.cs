// ABOUTME: Public API controller for retrieving translations and available languages.
// ABOUTME: All endpoints are AllowAnonymous — Blazor frontend fetches translations without auth.

using Asp.Versioning;
using Explore.API.Attributes;
using Explore.API.Hateoas;
using Explore.Application.Features.Localization.Requests.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Explore.API.Controllers;

[ApiVersion("0.1")]
[Route("api/[controller]")]
[ApiController]
[EndpointClassification(EndpointClass.Public)]
public class TranslationController : ControllerBase
{
    private readonly IMediator _mediator;

    public TranslationController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all translations for a language.
    /// </summary>
    [HttpGet("{languageCode}", Name = RouteNames.GetTranslationByLanguage)]
    [AllowAnonymous]
    [EndpointSummary("Get Translations")]
    [EndpointDescription("Returns all translation key-value pairs for the specified language code.")]
    [ProducesResponseType(typeof(Dictionary<string, string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Dictionary<string, string>>> GetTranslations(
        string languageCode,
        CancellationToken cancellationToken = default)
    {
        var translations = await _mediator.Send(
            new GetTranslationsQuery { LanguageCode = languageCode },
            cancellationToken);
        return Ok(translations);
    }

    /// <summary>
    /// Get available translation languages.
    /// </summary>
    [HttpGet("languages", Name = RouteNames.GetAvailableTranslationLanguages)]
    [AllowAnonymous]
    [EndpointSummary("Get Available Languages")]
    [EndpointDescription("Returns the list of language codes that have translations available.")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<string>>> GetLanguages(CancellationToken cancellationToken = default)
    {
        var languages = await _mediator.Send(new GetAvailableLanguagesQuery(), cancellationToken);
        return Ok(languages);
    }
}
