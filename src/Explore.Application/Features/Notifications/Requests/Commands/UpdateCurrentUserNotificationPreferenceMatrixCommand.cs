// ABOUTME: Command request for saving authenticated user notification preference cells.
// ABOUTME: Applies user-scope overrides while preserving required and locked category semantics.

using Explore.Application.DTOs.Notification;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Notifications.Requests.Commands;

public sealed class UpdateCurrentUserNotificationPreferenceMatrixCommand : IRequest<BaseCommandResponse<Guid>>
{
    public IReadOnlyList<UpdateNotificationPreferenceCellDto>? Cells { get; set; }
}
