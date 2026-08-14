// ABOUTME: Query request for reading the active instance paid-event policy.
// ABOUTME: Uses the existing instance setting authorization resource boundary.

using Explore.Application.Authorization;
using Explore.Application.DTOs.PaidEventPolicies;
using MediatR;

namespace Explore.Application.Features.PaidEventPolicies.Requests.Queries;

[AuthorizeResource(ResourceKinds.InstanceSetting, AuthorizationActions.InstanceSettings.View)]
public sealed record GetInstancePaidEventPolicyQuery : IRequest<PaidEventPolicyDto?>, ISecureRequest
{
    public const string SettingKey = "paid-event-policy";

    string? ISecureRequest.ResourceId => SettingKey;

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => new Dictionary<string, object>
    {
        ["settingKey"] = SettingKey
    };
}
