using System;
using System.Collections.Generic;

namespace Explore.Application.DTOs.Event;

/// <summary>
/// DTO for creating an event along with its sessions in a single transaction.
/// At least one session is required. FirstSessionDate and LastSessionDate are
/// computed from the sessions by the handler.
/// </summary>
public class CreateEventWithSessionsDto
{
    /// <summary>
    /// Event title (required).
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// Optional event description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Optional event slug. If not provided, generated from title.
    /// </summary>
    public string? Slug { get; set; }

    /// <summary>
    /// Event type ID (required).
    /// </summary>
    public int EventTypeId { get; set; }

    /// <summary>
    /// Audience gender ID (required).
    /// </summary>
    public int AudienceGenderId { get; set; }

    /// <summary>
    /// Audience age ID (required).
    /// </summary>
    public int AudienceAgeId { get; set; }

    /// <summary>
    /// Optional organization ID. If null, event is created under user's personal actor.
    /// If provided, user must be an admin of the organization.
    /// </summary>
    public Guid? OrganizationId { get; set; }

    /// <summary>
    /// Optional event price.
    /// </summary>
    public decimal? Price { get; set; }

    /// <summary>
    /// Optional currency code (3 letters, e.g., "EUR", "USD").
    /// </summary>
    public string? CurrencyCode { get; set; }

    /// <summary>
    /// Optional featured image ID (must be a previously uploaded StorageObject).
    /// </summary>
    public Guid? FeaturedImageId { get; set; }

    /// <summary>
    /// Whether registration is required for this event.
    /// </summary>
    public bool IsRegistrationRequired { get; set; }

    /// <summary>
    /// Optional external registration URL.
    /// </summary>
    public string? ExternalRegistrationUrl { get; set; }

    /// <summary>
    /// Event status ID. Default: 1 (Draft).
    /// </summary>
    public int EventStatusId { get; set; } = 1;

    /// <summary>
    /// Visibility type ID. Default: 1 (Public).
    /// </summary>
    public int VisibilityTypeId { get; set; } = 1;

    /// <summary>
    /// Event format ID. Default: 1 (In-Person).
    /// </summary>
    public int EventFormatId { get; set; } = 1;

    /// <summary>
    /// Optional madhab ID (Islamic jurisprudence school).
    /// </summary>
    public int? MadhabId { get; set; }

    /// <summary>
    /// Optional timezone (IANA format, e.g., "Europe/Brussels").
    /// </summary>
    public string? Timezone { get; set; }

    /// <summary>
    /// Optional event URL.
    /// </summary>
    public string? EventUrl { get; set; }

    /// <summary>
    /// Sessions for this event (at least one required).
    /// FirstSessionDate and LastSessionDate will be computed from these sessions.
    /// </summary>
    public List<CreateEventSessionForEventDto> Sessions { get; set; } = new();
}
