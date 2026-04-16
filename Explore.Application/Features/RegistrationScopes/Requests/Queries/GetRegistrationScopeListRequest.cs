// ABOUTME: MediatR query request for fetching all registration scopes.
// ABOUTME: Returns list of RegistrationScopeListDto (Event, Day, SessionSelection).

using Explore.Application.DTOs.RegistrationScope;
using MediatR;

namespace Explore.Application.Features.RegistrationScopes.Requests.Queries;

public class GetRegistrationScopeListRequest : IRequest<List<RegistrationScopeListDto>>
{
}
