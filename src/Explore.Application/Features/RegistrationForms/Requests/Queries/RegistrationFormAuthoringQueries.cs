// ABOUTME: Defines explicit registration workflow, form, version, and publish-preflight reads.
// ABOUTME: Routes every authoring read through event-scoped manage-workflow authorization.

using Explore.Application.Authorization;
using Explore.Application.DTOs.RegistrationForms;
using MediatR;

namespace Explore.Application.Features.RegistrationForms.Requests.Queries;

public interface IRegistrationFormAuthoringQuery<out TResponse> : IRequest<TResponse>, ISecureRequest
{
    Guid EventId { get; }

    string? ISecureRequest.ResourceId => EventId == Guid.Empty ? null : EventId.ToString();

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => new Dictionary<string, object>
    {
        ["eventId"] = EventId.ToString()
    };
}

public interface IRegistrationFormScopedQuery<out TResponse> : IRegistrationFormAuthoringQuery<TResponse>
{
    Guid FormId { get; }

    string? ISecureRequest.ResourceId => FormId == Guid.Empty ? null : FormId.ToString();

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => new Dictionary<string, object>
    {
        ["eventId"] = EventId.ToString(),
        ["formId"] = FormId.ToString()
    };
}

public interface IRegistrationFormVersionScopedQuery<out TResponse> : IRegistrationFormScopedQuery<TResponse>
{
    Guid VersionId { get; }

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => new Dictionary<string, object>
    {
        ["eventId"] = EventId.ToString(),
        ["formId"] = FormId.ToString(),
        ["versionId"] = VersionId.ToString()
    };
}

[AuthorizeResource(ResourceKinds.Event, AuthorizationActions.Events.ManageRegistrationWorkflow)]
public sealed record GetRegistrationWorkflowQuery(Guid EventId, string Purpose)
    : IRegistrationFormAuthoringQuery<RegistrationWorkflowDto?>;

[AuthorizeResource(ResourceKinds.RegistrationForm, AuthorizationActions.RegistrationForms.View)]
public sealed record GetRegistrationFormQuery(Guid EventId, Guid FormId)
    : IRegistrationFormScopedQuery<RegistrationFormDto?>;

[AuthorizeResource(ResourceKinds.RegistrationForm, AuthorizationActions.RegistrationForms.View)]
public sealed record GetRegistrationFormVersionQuery(Guid EventId, Guid FormId, Guid VersionId)
    : IRegistrationFormVersionScopedQuery<RegistrationFormVersionDto?>;

[AuthorizeResource(ResourceKinds.RegistrationForm, AuthorizationActions.RegistrationForms.Preflight)]
public sealed record GetRegistrationFormPublishPreflightQuery(Guid EventId, Guid FormId, Guid VersionId)
    : IRegistrationFormVersionScopedQuery<RegistrationFormPublishPreflightDto?>;
