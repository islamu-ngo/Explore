// ABOUTME: Intent-first registration payload - captures why a user is registering (scope) plus scope-specific data.
// ABOUTME: Session children are derived by CreateEventRegistrationCommandHandler based on scope; do not set EventSessionId here.

using System;
using System.Collections.Generic;

namespace Explore.Application.DTOs.EventRegistration;

public class CreateEventRegistrationDto
{
    public Guid EventId { get; set; }
    public Guid UserId { get; set; }

    /// <summary>
    /// Registration scope lookup id (Event / Day / SessionSelection). Must be allowed by the event's
    /// <see cref="Explore.Domain.EventRegistrationPolicy"/>.
    /// </summary>
    public int RegistrationScopeId { get; set; }

    /// <summary>
    /// Required when <see cref="RegistrationScopeId"/> maps to <see cref="Explore.Domain.Enums.RegistrationScopeEnum.Day"/>.
    /// Must reference an <see cref="Explore.Domain.EventDay"/> belonging to <see cref="EventId"/>.
    /// </summary>
    public Guid? SelectedEventDayId { get; set; }

    /// <summary>
    /// Required (non-empty) when <see cref="RegistrationScopeId"/> maps to
    /// <see cref="Explore.Domain.Enums.RegistrationScopeEnum.SessionSelection"/>. All ids must reference sessions
    /// belonging to <see cref="EventId"/>.
    /// </summary>
    public IReadOnlyList<Guid> SelectedSessionIds { get; set; } = Array.Empty<Guid>();

    public int? ApprovalStatusId { get; set; }
}
