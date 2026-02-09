using System;
using System.Collections.Generic;
using Explore.Application.DTOs.EventSessionLanguage;
using MediatR;

namespace Explore.Application.Features.EventSessionLanguages.Requests.Queries;

public class GetLanguagesBySessionRequest : IRequest<List<EventSessionLanguageListDto>>
{
    public Guid EventSessionId { get; set; }
}
