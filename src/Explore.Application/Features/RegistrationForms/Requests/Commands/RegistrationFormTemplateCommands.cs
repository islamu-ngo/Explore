// ABOUTME: Defines registration-form template catalog write requests.
// ABOUTME: Separates template creation authority from event-scoped instantiation provenance.

using Explore.Application.Authorization;
using Explore.Application.DTOs.RegistrationForms;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.RegistrationForms.Requests.Commands;

[AuthorizeResource(ResourceKinds.RegistrationForm, AuthorizationActions.RegistrationForms.Create)]
public sealed record CreateRegistrationFormTemplateCommand(RegistrationFormTemplateInput Input)
    : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    string? ISecureRequest.ResourceId => Input.SourceRegistrationFormId == Guid.Empty
        ? null
        : Input.SourceRegistrationFormId.ToString();
}

[AuthorizeResource(ResourceKinds.Event, AuthorizationActions.Events.ManageRegistrationWorkflow)]
public sealed record InstantiateRegistrationFormTemplateCommand(
    Guid TemplateId,
    InstantiateRegistrationFormTemplateInput Input)
    : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    string? ISecureRequest.ResourceId => Input.EventId == Guid.Empty ? null : Input.EventId.ToString();

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => new Dictionary<string, object>
    {
        ["templateId"] = TemplateId.ToString(),
        ["eventId"] = Input.EventId.ToString(),
        ["workflowId"] = Input.WorkflowId.ToString()
    };
}
