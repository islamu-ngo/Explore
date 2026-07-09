// ABOUTME: Command request for setting the authenticated user's non-essential notification mute flag.
// ABOUTME: Preserves individual channel choices while toggling the profile-level mute state.

using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Notifications.Requests.Commands;

public sealed class SetCurrentUserNotificationPreferenceMuteCommand : IRequest<BaseCommandResponse<Guid>>
{
    public bool IsMuted { get; set; }
}
