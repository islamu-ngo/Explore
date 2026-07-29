// ABOUTME: MediatR query for fetching all languages in a session.
// ABOUTME: Returns IEnumerable<LanguageDto>.
using System;
using System.Collections.Generic;
using Explore.Application.Authorization;
using Explore.Application.DTOs.EventSessionLanguage;
using MediatR;

namespace Explore.Application.Features.EventSessionLanguages.Requests.Queries;

public class GetLanguagesBySessionRequest : IRequest<List<EventSessionLanguageListDto>>
{
    public Guid EventSessionId { get; set; }
}

[AuthorizeResource(ResourceKinds.Event, AuthorizationActions.Events.ViewManagement)]
public sealed class GetManagedLanguagesBySessionRequest : IRequest<List<EventSessionLanguageListDto>>, ISecureRequest
{
    public Guid EventId { get; set; }
    public Guid EventSessionId { get; set; }

    string? ISecureRequest.ResourceId => EventId.ToString();
}
