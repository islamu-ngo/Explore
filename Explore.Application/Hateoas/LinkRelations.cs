// ABOUTME: Central link-relation catalog for HAL resources emitted by the API.
// ABOUTME: Separates standard IANA relations from ISLAMU Event custom action relations.

namespace Explore.Application.Hateoas;

/// <summary>
/// Standard link relation types as defined by IANA and custom relations for ISLAMU Event.
/// See: https://www.iana.org/assignments/link-relations/link-relations.xhtml
/// </summary>
public static class LinkRelations
{
    #region IANA Standard Relations (RFC 8288)

    /// <summary>
    /// Conveys an identifier for the link's context (current resource).
    /// </summary>
    public const string Self = "self";

    /// <summary>
    /// The target IRI points to a resource which represents the collection resource.
    /// </summary>
    public const string Collection = "collection";

    /// <summary>
    /// The target IRI points to a resource that is a member of the collection.
    /// </summary>
    public const string Item = "item";

    /// <summary>
    /// An IRI that refers to the furthest preceding resource in a series.
    /// </summary>
    public const string First = "first";

    /// <summary>
    /// An IRI that refers to the furthest following resource in a series.
    /// </summary>
    public const string Last = "last";

    /// <summary>
    /// Indicates that the link's context is a part of a series, and the previous in the series is the link target.
    /// </summary>
    public const string Prev = "prev";

    /// <summary>
    /// Indicates that the link's context is a part of a series, and the next in the series is the link target.
    /// </summary>
    public const string Next = "next";

    /// <summary>
    /// Refers to a resource that can be used to edit the link's context.
    /// </summary>
    public const string Edit = "edit";

    /// <summary>
    /// Identifies a related resource.
    /// </summary>
    public const string Related = "related";

    /// <summary>
    /// Refers to a resource that can be used to search through the link's context and related resources.
    /// </summary>
    public const string Search = "search";

    /// <summary>
    /// The target resource represents the canonical URI for the context.
    /// </summary>
    public const string Canonical = "canonical";

    /// <summary>
    /// Refers to a parent document in a hierarchy of documents.
    /// </summary>
    public const string Up = "up";

    /// <summary>
    /// Points to a resource containing the version history for the context.
    /// </summary>
    public const string VersionHistory = "version-history";

    /// <summary>
    /// Indicates a resource where payment is accepted.
    /// </summary>
    public const string Payment = "payment";

    #endregion

    #region Common CRUD Operations

    /// <summary>
    /// Link to create a new resource in a collection.
    /// </summary>
    public const string Create = "create";

    /// <summary>
    /// Link to update the current resource.
    /// </summary>
    public const string Update = "update";

    /// <summary>
    /// Link to delete the current resource.
    /// </summary>
    public const string Delete = "delete";

    #endregion

    #region ISLAMU Event Custom Relations

    /// <summary>
    /// Events belonging to an organization or actor.
    /// </summary>
    public const string Events = "events";

    /// <summary>
    /// Sessions belonging to an event.
    /// </summary>
    public const string Sessions = "sessions";

    /// <summary>
    /// Program overview for an event, composed from program sections and sessions.
    /// </summary>
    public const string Program = "program";

    /// <summary>
    /// Server-backed program summary for an event.
    /// </summary>
    public const string ProgramSummary = "program-summary";

    /// <summary>
    /// Action link for adding a session to an event program.
    /// </summary>
    public const string AddSession = "add-session";

    /// <summary>
    /// Server-owned defaults and option lists for adding a session to an event program.
    /// </summary>
    public const string SessionCreateContext = "session-create-context";

    /// <summary>
    /// Action link for assigning sessions to a program section, track, devroom, or stage.
    /// </summary>
    public const string AssignSession = "assign-session";

    /// <summary>
    /// Action link for adding a program section, track, devroom, or stage to an event.
    /// </summary>
    public const string AddSessionGroup = "add-session-group";

    /// <summary>
    /// Speakers for a session.
    /// </summary>
    public const string Speakers = "speakers";

    /// <summary>
    /// Agenda items for a session.
    /// </summary>
    public const string AgendaItems = "agenda-items";

    /// <summary>
    /// Members of an organization.
    /// </summary>
    public const string Members = "members";

    /// <summary>
    /// Team members with operational roles for an event.
    /// </summary>
    public const string Team = "team";

    /// <summary>
    /// Registration action for an event or session.
    /// </summary>
    public const string Registration = "registration";

    /// <summary>
    /// Registrations for an event or session.
    /// </summary>
    public const string Registrations = "registrations";

    /// <summary>
    /// Categories assigned to an event.
    /// </summary>
    public const string Categories = "categories";

    /// <summary>
    /// Tags assigned to an event.
    /// </summary>
    public const string Tags = "tags";

    /// <summary>
    /// Location of a session or event.
    /// </summary>
    public const string Location = "location";

    /// <summary>
    /// Parent organization.
    /// </summary>
    public const string Organization = "organization";

    /// <summary>
    /// Parent event.
    /// </summary>
    public const string Event = "event";

    /// <summary>
    /// Actor (user or organization) associated with a resource.
    /// </summary>
    public const string Actor = "actor";

    /// <summary>
    /// Featured image for a resource.
    /// </summary>
    public const string FeaturedImage = "featured-image";

    /// <summary>
    /// Parent category (for hierarchical categories).
    /// </summary>
    public const string Parent = "parent";

    /// <summary>
    /// Child categories or subcategories.
    /// </summary>
    public const string Children = "children";

    /// <summary>
    /// Program sections, tracks, devrooms, or stages for an event.
    /// </summary>
    public const string SessionGroups = "session-groups";

    /// <summary>
    /// Tag type that a tag belongs to.
    /// </summary>
    public const string TagType = "tag-type";

    /// <summary>
    /// Languages for an event session.
    /// </summary>
    public const string Languages = "languages";

    /// <summary>
    /// Publish action for draft resources.
    /// </summary>
    public const string Publish = "publish";

    /// <summary>
    /// Readiness validation for publishing draft resources.
    /// </summary>
    public const string PublishReadiness = "publish-readiness";

    /// <summary>
    /// Cancel action for events or registrations.
    /// </summary>
    public const string Cancel = "cancel";

    /// <summary>
    /// Archive action for lifecycle-managed resources.
    /// </summary>
    public const string Archive = "archive";

    /// <summary>
    /// Revoke action for grants, invitations, or other auditable authority records.
    /// </summary>
    public const string Revoke = "revoke";

    /// <summary>
    /// Action link for cancelling an AI provider run before it completes.
    /// </summary>
    public const string CancelRun = "cancel-run";

    /// <summary>
    /// Action link for sending a message into an AI assistant conversation.
    /// </summary>
    public const string SendMessage = "send-message";

    /// <summary>
    /// Action link for confirming an AI-proposed action before execution.
    /// </summary>
    public const string ConfirmAction = "confirm-action";

    /// <summary>
    /// Action link for rejecting an AI-proposed action without side effects.
    /// </summary>
    public const string RejectAction = "reject-action";

    #endregion
}
