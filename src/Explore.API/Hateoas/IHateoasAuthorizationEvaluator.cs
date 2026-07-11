namespace Explore.API.Hateoas;

using System.Security.Claims;
using Explore.Application.Hateoas;
using Microsoft.AspNetCore.Http;

public interface IHateoasAuthorizationEvaluator
{
    Task<IReadOnlyList<bool>> AreLinksAllowedAsync(IReadOnlyList<LinkDefinition> definitions, ClaimsPrincipal? user, HttpContext httpContext);
}
