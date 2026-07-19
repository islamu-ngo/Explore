// ABOUTME: Materializes one immutable fanout occurrence for one explicit recipient.
// ABOUTME: Resolves current persisted email, preference, and location authority before atomic graph creation.

using Explore.Application.Contracts.Notifications;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Notifications;
using Explore.Domain;
using Explore.Domain.Enums;

namespace Explore.Application.Services;

public sealed class NotificationFanoutRecipientMaterializationService(
    IUserRepository userRepository,
    INotificationPreferenceResolver preferenceResolver,
    IFanoutAttendeeLocationAuthorizationService locationAuthorizationService,
    NotificationFanoutRecipientTemplateFactory templateFactory,
    IRecipientNotificationMaterializer materializer)
{
    public async Task<RecipientNotificationMaterializationResult> MaterializeAsync(
        NotificationFanoutOccurrence occurrence,
        Guid recipientUserId,
        CancellationToken cancellationToken = default)
    {
        NotificationFanoutRecipientTemplate template = templateFactory.Parse(occurrence);
        User? user = await userRepository.GetUserWithDetails(recipientUserId, cancellationToken);
        (string? verifiedEmail, string? emailSkipReason) = ResolveVerifiedEmail(user, recipientUserId);
        NotificationPreferenceDecision emailPreference = await preferenceResolver.ResolveAsync(
            new NotificationPreferenceResolveRequest(
                occurrence.TenantId,
                recipientUserId,
                OrganizationId: null,
                GroupId: null,
                NotificationPreferenceCategoryCodes.EventUpdates,
                NotificationPreferenceChannelCodes.Email),
            cancellationToken);
        FanoutAttendeeLocationAuthorizationResult? locationAuthorization = null;
        if (template.LocationForDisclosure is { } location)
        {
            locationAuthorization = await locationAuthorizationService.AuthorizeAsync(
                new FanoutAttendeeLocationAuthorizationRequest(
                    occurrence.TenantId,
                    occurrence.EventId,
                    recipientUserId,
                    location.EventLocationId,
                    location.RoomId),
                cancellationToken);
        }

        RecipientNotificationMaterialization request = templateFactory.CreateMaterialization(
            occurrence,
            template,
            recipientUserId,
            verifiedEmail,
            emailPreference.IsEnabled,
            emailSkipReason,
            locationAuthorization);
        return await materializer.MaterializeAsync(request, cancellationToken);
    }

    private static (string? Email, string? SkipReason) ResolveVerifiedEmail(User? user, Guid recipientUserId)
    {
        if (user is null || user.Id != recipientUserId || user.IsDeleted || user.Pii is null)
        {
            return (null, "recipient_deleted_or_missing");
        }

        if (user.EmailVerified != true)
        {
            return (null, "recipient_email_unverified");
        }

        string email = user.Pii.Email?.Trim() ?? string.Empty;
        return string.IsNullOrWhiteSpace(email)
            ? (null, "recipient_email_missing")
            : (email, null);
    }
}
