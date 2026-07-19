// ABOUTME: HAL policy for instance-wide SMTP processor controls.
// ABOUTME: Emits only state-valid pause, resume, set-rate, and clear-rate instance-admin affordances.

using System.Security.Claims;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.EmailDispatch;
using Explore.Application.Features.EmailDispatch;
using Explore.Application.Hateoas;

namespace Explore.API.Hateoas.Policies;

public sealed class EmailDispatchProcessorControlDetailLinkPolicy
    : ILinkPolicy<EmailDispatchProcessorControlDto>
{
    public IEnumerable<LinkDefinition> GetLinks(EmailDispatchProcessorControlDto dto, ClaimsPrincipal? user)
    {
        yield return Link("self", RouteNames.GetEmailDispatchProcessorControl, "GET",
            AuthorizationActions.InstanceSettings.View);

        yield return dto.IsPaused
            ? Link("resume", RouteNames.ResumeEmailDispatchProcessor, "DELETE",
                AuthorizationActions.InstanceSettings.Update)
            : Link("pause", RouteNames.PauseEmailDispatchProcessor, "PUT",
                AuthorizationActions.InstanceSettings.Update);

        yield return Link("set-rate-limit", RouteNames.SetEmailDispatchGlobalRateLimitOverride, "PUT",
            AuthorizationActions.InstanceSettings.Update);

        if (dto.GlobalSmtpRateLimitPerMinuteOverride.HasValue)
        {
            yield return Link("clear-rate-limit", RouteNames.ClearEmailDispatchGlobalRateLimitOverride, "DELETE",
                AuthorizationActions.InstanceSettings.Update);
        }
    }

    private static LinkDefinition Link(string relation, string routeName, string method, string action) =>
        new LinkDefinition(relation, routeName, null, method, relation, RequiresAuth: true)
            .RequirePermission(
                action,
                ResourceKinds.InstanceSetting,
                EmailDispatchProcessorControl.SettingKey,
                new Dictionary<string, object>
                {
                    ["settingKey"] = EmailDispatchProcessorControl.SettingKey
                });
}

public sealed class EmailDispatchProcessorControlCollectionLinkPolicy
    : ICollectionLinkPolicy<EmailDispatchProcessorControlDto>
{
    public IEnumerable<LinkDefinition> GetItemLinks(EmailDispatchProcessorControlDto dto, ClaimsPrincipal? user) => [];
    public IEnumerable<LinkDefinition> GetCollectionLinks(ClaimsPrincipal? user) => [];
}
