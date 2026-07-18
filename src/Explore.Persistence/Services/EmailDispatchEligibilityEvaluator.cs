// ABOUTME: Atomically revalidates current recipient authority immediately before SMTP provider handoff.
// ABOUTME: Refreshes verified addresses or settles linked outbox, attempt, receipt, and delivery rows as skipped.

using System.Data;
using Explore.Application.Contracts.Notifications;
using Explore.Application.Contracts.Services;
using Explore.Application.Notifications;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using Explore.Persistence.QueryFilters;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Services;

public sealed class EmailDispatchEligibilityEvaluator(
    ExploreDbContext dbContext,
    NotificationDeliveryPolicyResolver policyResolver,
    INotificationPreferenceResolver preferenceResolver) : IEmailDispatchEligibilityEvaluator
{
    private const string ProviderHandoffStarted = "provider_handoff_started";
    private const string ProviderHandoffMessage = "SMTP provider handoff started; automatic resend is suppressed until the attempt is durably settled.";
    private const string SkipMessage = "Email delivery was suppressed by current dispatch eligibility before provider handoff.";

    public async Task<EmailDispatchEligibilityResult> EvaluateAndBeginProviderHandoffAsync(
        EmailDispatchEligibilityRequest request,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        var dispatch = await dbContext.EmailDispatchOutbox
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .SingleOrDefaultAsync(outbox =>
                outbox.TenantId == request.TenantId &&
                outbox.Id == request.OutboxId,
                cancellationToken);

        if (dispatch is null
            || dispatch.ContentRedactedAt is not null
            || dispatch.Status != EmailDispatchStatus.Processing
            || dispatch.ProcessingLeaseToken != request.ProcessingLeaseToken
            || dispatch.AttemptCount != request.AttemptNumber)
        {
            await transaction.CommitAsync(cancellationToken);
            return new EmailDispatchEligibilityResult(EmailDispatchEligibilityOutcome.LostClaim, null, null);
        }

        var delivery = await dbContext.NotificationDeliveries
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .Include(value => value.DeliveryPolicy)
            .Include(value => value.NotificationIntent)
            .SingleOrDefaultAsync(value =>
                value.TenantId == request.TenantId &&
                value.EmailDispatchOutboxId == request.OutboxId &&
                value.ChannelId == (int)NotificationPreferenceChannelEnum.Email,
                cancellationToken);

        if (delivery is null || delivery.NotificationIntent is null || delivery.DeliveryPolicy is null)
        {
            return await SkipAsync(dispatch, delivery, request, "delivery_authority_missing", cancellationToken);
        }

        if (delivery.StatusId == (int)NotificationDeliveryStatusEnum.Superseded)
        {
            return await SkipAsync(dispatch, delivery, request, "delivery_superseded", cancellationToken);
        }

        if (delivery.StatusId is not ((int)NotificationDeliveryStatusEnum.Pending)
            and not ((int)NotificationDeliveryStatusEnum.Queued))
        {
            return await SkipAsync(dispatch, delivery, request, "delivery_state_ineligible", cancellationToken);
        }

        var policy = policyResolver.Resolve(
            delivery.DeliveryPolicyId,
            delivery.DeliveryPolicy.MasterCode,
            delivery.PolicyVersion);
        if (!policy.IsSupported)
        {
            return await SkipAsync(dispatch, delivery, request, policy.SkipReason!, cancellationToken);
        }

        var tenantActive = await dbContext.Tenants
            .AsNoTracking()
            .AnyAsync(tenant =>
                tenant.Id == request.TenantId &&
                tenant.TenantStatusId == (int)TenantStatusEnum.Active,
                cancellationToken);
        if (!tenantActive)
        {
            return await SkipAsync(dispatch, delivery, request, "tenant_inactive", cancellationToken);
        }

        var tenantPaused = await dbContext.EmailDispatchTenantControls
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .AsNoTracking()
            .AnyAsync(control => control.TenantId == request.TenantId && control.IsPaused, cancellationToken);
        if (tenantPaused)
        {
            dispatch.Status = EmailDispatchStatus.Pending;
            dispatch.AttemptCount = Math.Max(0, dispatch.AttemptCount - 1);
            dispatch.ProcessingStartedAt = null;
            dispatch.ProcessingLeaseToken = null;
            dispatch.UpdatedAt = request.EvaluatedAt;
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new EmailDispatchEligibilityResult(EmailDispatchEligibilityOutcome.TenantPaused, null, "tenant_paused");
        }

        var tenantUser = await dbContext.TenantUsers
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .AsNoTracking()
            .SingleOrDefaultAsync(value =>
                value.TenantId == request.TenantId &&
                value.UserId == dispatch.RecipientUserId,
                cancellationToken);
        if (tenantUser is null || tenantUser.StatusId != (int)TenantUserStatusEnum.Active)
        {
            return await SkipAsync(dispatch, delivery, request, "recipient_membership_inactive", cancellationToken);
        }

        var user = await dbContext.Users
            .AsNoTracking()
            .Include(value => value.Pii)
            .SingleOrDefaultAsync(value => value.Id == dispatch.RecipientUserId, cancellationToken);
        if (user is null)
        {
            return await SkipAsync(dispatch, delivery, request, "recipient_deleted", cancellationToken);
        }

        string recipientEmail;
        if (policy.UsesInvitationDestination)
        {
            if (dispatch.RecipientAddressSource != RecipientAddressSource.ManagedTenantAdministratorInvitation
                || dispatch.ManagedTenantProvisioningOperationId is not Guid operationId)
            {
                return await SkipAsync(dispatch, delivery, request, "invitation_authority_missing", cancellationToken);
            }

            var invitationAuthorized = await dbContext.ManagedTenantProvisioningOperations
                .AsNoTracking()
                .AnyAsync(operation =>
                    operation.Id == operationId &&
                    operation.Status == ManagedTenantProvisioningStatus.Succeeded &&
                    operation.TenantId == request.TenantId &&
                    operation.TenantAdministratorUserId == dispatch.RecipientUserId,
                    cancellationToken);
            if (!invitationAuthorized || string.IsNullOrWhiteSpace(dispatch.RecipientEmail))
            {
                return await SkipAsync(dispatch, delivery, request, "invitation_authority_invalid", cancellationToken);
            }

            recipientEmail = dispatch.RecipientEmail.Trim();
        }
        else
        {
            if (dispatch.RecipientAddressSource != RecipientAddressSource.TenantUserVerifiedEmail)
            {
                return await SkipAsync(dispatch, delivery, request, "recipient_address_source_mismatch", cancellationToken);
            }

            recipientEmail = user.Email.Trim();
            if (user.EmailVerified != true || string.IsNullOrWhiteSpace(recipientEmail))
            {
                return await SkipAsync(dispatch, delivery, request, "recipient_email_unverified", cancellationToken);
            }
        }

        var consentReason = await ResolveConsentSkipReasonAsync(policy, delivery, dispatch, cancellationToken);
        if (consentReason is not null)
        {
            return await SkipAsync(dispatch, delivery, request, consentReason, cancellationToken);
        }

        if (policy.HonorsPreference)
        {
            if (string.IsNullOrWhiteSpace(delivery.PreferenceCategoryCode))
            {
                return await SkipAsync(dispatch, delivery, request, "notification_preference_category_missing", cancellationToken);
            }

            var preference = await preferenceResolver.ResolveAsync(
                new NotificationPreferenceResolveRequest(
                    request.TenantId,
                    dispatch.RecipientUserId,
                    null,
                    null,
                    delivery.PreferenceCategoryCode,
                    NotificationPreferenceChannelCodes.Email),
                cancellationToken);
            if (!preference.IsEnabled)
            {
                return await SkipAsync(dispatch, delivery, request, "recipient_notification_preference_disabled", cancellationToken);
            }

            var legacyCategory = ResolveLegacyPreferenceCategory(dispatch.Kind);
            if (legacyCategory is not null)
            {
                var legacyDisabled = await dbContext.UserNotificationPreferences
                    .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
                    .AsNoTracking()
                    .AnyAsync(value =>
                        value.TenantId == request.TenantId &&
                        value.UserId == dispatch.RecipientUserId &&
                        value.Category == legacyCategory &&
                        !value.IsEnabled,
                        cancellationToken);
                if (legacyDisabled)
                {
                    return await SkipAsync(dispatch, delivery, request, "recipient_unsubscribed", cancellationToken);
                }
            }
        }

        dispatch.RecipientEmail = recipientEmail;
        dispatch.UpdatedAt = request.EvaluatedAt;
        var receipt = await UpsertReceiptAsync(dispatch, request, EmailDispatchReceiptStatus.Processing, null, cancellationToken);
        await UpsertAttemptAsync(dispatch, request, EmailDispatchAttemptOutcome.Unknown, ProviderHandoffStarted, ProviderHandoffMessage, null, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new EmailDispatchEligibilityResult(
            EmailDispatchEligibilityOutcome.Eligible,
            recipientEmail,
            null,
            receipt.Id);
    }

    private async Task<string?> ResolveConsentSkipReasonAsync(
        NotificationDeliveryPolicyResolution policy,
        NotificationDelivery delivery,
        EmailDispatchOutbox dispatch,
        CancellationToken cancellationToken)
    {
        if (policy.ConsentRequirement == EmailDispatchConsentRequirement.None)
        {
            return null;
        }

        if (delivery.NotificationIntent?.ReportId is not Guid reportId)
        {
            return "report_consent_source_missing";
        }

        var report = await dbContext.EventReports
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .AsNoTracking()
            .SingleOrDefaultAsync(value =>
                value.TenantId == dispatch.TenantId &&
                value.Id == reportId &&
                value.ReporterUserId == dispatch.RecipientUserId,
                cancellationToken);
        if (report is null)
        {
            return "report_consent_source_missing";
        }

        return policy.ConsentRequirement switch
        {
            EmailDispatchConsentRequirement.ReportCaseUpdates => "report_case_update_consent_unavailable",
            EmailDispatchConsentRequirement.ReportFollowUpContact when
                !string.Equals(delivery.ConsentPurpose, ReportEmailConsentPurposeCodes.FollowUpContact, StringComparison.Ordinal)
                => "report_consent_purpose_mismatch",
            EmailDispatchConsentRequirement.ReportFollowUpContact when !report.ReporterContactConsent
                => "report_follow_up_consent_withdrawn",
            _ => null
        };
    }

    private async Task<EmailDispatchEligibilityResult> SkipAsync(
        EmailDispatchOutbox dispatch,
        NotificationDelivery? delivery,
        EmailDispatchEligibilityRequest request,
        string reason,
        CancellationToken cancellationToken)
    {
        dispatch.Status = EmailDispatchStatus.Skipped;
        dispatch.NextAttemptAt = null;
        dispatch.ProcessingStartedAt = null;
        dispatch.ProcessingLeaseToken = null;
        dispatch.LastFailureCategory = reason;
        dispatch.LastError = SkipMessage;
        dispatch.LastFailureAt = request.EvaluatedAt;
        dispatch.UpdatedAt = request.EvaluatedAt;

        if (delivery is not null)
        {
            delivery.StatusId = (int)NotificationDeliveryStatusEnum.Skipped;
            delivery.ProviderStatus = "skipped";
            delivery.FailureCategory = reason;
            delivery.CompletedAt = request.EvaluatedAt;
            delivery.UpdatedAt = request.EvaluatedAt;
        }

        var receipt = await UpsertReceiptAsync(dispatch, request, EmailDispatchReceiptStatus.Skipped, reason, cancellationToken);
        await UpsertAttemptAsync(
            dispatch,
            request,
            EmailDispatchAttemptOutcome.Skipped,
            reason,
            SkipMessage,
            request.EvaluatedAt,
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await dbContext.Database.CommitTransactionAsync(cancellationToken);
        return new EmailDispatchEligibilityResult(EmailDispatchEligibilityOutcome.Skipped, null, reason, receipt.Id);
    }

    private async Task<EmailDispatchReceipt> UpsertReceiptAsync(
        EmailDispatchOutbox dispatch,
        EmailDispatchEligibilityRequest request,
        EmailDispatchReceiptStatus status,
        string? reason,
        CancellationToken cancellationToken)
    {
        var receipt = await dbContext.EmailDispatchReceipts
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .SingleOrDefaultAsync(value =>
                value.TenantId == request.TenantId &&
                value.EmailDispatchOutboxId == request.OutboxId,
                cancellationToken);
        if (receipt is null)
        {
            receipt = new EmailDispatchReceipt
            {
                Id = Guid.CreateVersion7(),
                TenantId = request.TenantId,
                PublishEventId = dispatch.PublishEventId,
                EmailDispatchOutboxId = dispatch.Id,
                ConsumerId = request.ConsumerId,
                FirstSeenAt = request.EvaluatedAt,
                CreatedAt = request.EvaluatedAt
            };
            await dbContext.EmailDispatchReceipts.AddAsync(receipt, cancellationToken);
        }

        receipt.Status = status;
        receipt.ConsumerId = request.ConsumerId;
        receipt.ProcessingStartedAt = status == EmailDispatchReceiptStatus.Processing ? request.EvaluatedAt : null;
        receipt.CompletedAt = null;
        receipt.FailedAt = status == EmailDispatchReceiptStatus.Skipped ? request.EvaluatedAt : null;
        receipt.FailureCode = reason;
        receipt.FailureMessage = reason is null ? null : SkipMessage;
        receipt.ProviderMessageId = null;
        receipt.UpdatedAt = request.EvaluatedAt;
        return receipt;
    }

    private async Task UpsertAttemptAsync(
        EmailDispatchOutbox dispatch,
        EmailDispatchEligibilityRequest request,
        EmailDispatchAttemptOutcome outcome,
        string failureCategory,
        string message,
        DateTime? completedAt,
        CancellationToken cancellationToken)
    {
        var attempt = await dbContext.EmailDispatchAttempts
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .SingleOrDefaultAsync(value =>
                value.TenantId == request.TenantId &&
                value.EmailDispatchOutboxId == request.OutboxId &&
                value.AttemptNumber == request.AttemptNumber,
                cancellationToken);
        if (attempt is null)
        {
            attempt = new EmailDispatchAttempt
            {
                Id = Guid.CreateVersion7(),
                TenantId = request.TenantId,
                EmailDispatchOutboxId = dispatch.Id,
                AttemptNumber = request.AttemptNumber,
                StartedAt = request.EvaluatedAt,
                CreatedAt = request.EvaluatedAt
            };
            await dbContext.EmailDispatchAttempts.AddAsync(attempt, cancellationToken);
        }

        attempt.Outcome = outcome;
        attempt.CompletedAt = completedAt;
        attempt.FailureCategory = failureCategory;
        attempt.SanitizedErrorMessage = message;
        attempt.ProviderMessageId = null;
        attempt.CorrelationId = dispatch.CorrelationId;
        attempt.UpdatedAt = request.EvaluatedAt;
    }

    private static string? ResolveLegacyPreferenceCategory(EmailDispatchKind kind) => kind switch
    {
        EmailDispatchKind.RegistrationConfirmation => NotificationPreferenceCategories.RegistrationConfirmations,
        EmailDispatchKind.EventReminder => NotificationPreferenceCategories.EventReminders,
        EmailDispatchKind.OrganizerNotification => NotificationPreferenceCategories.OrganizerAnnouncements,
        EmailDispatchKind.RegistrationApproved
            or EmailDispatchKind.RegistrationRejected
            or EmailDispatchKind.WaitlistPromoted
            or EmailDispatchKind.RegistrationCancelled
            or EmailDispatchKind.RegistrationRevoked
            or EmailDispatchKind.EventCancelled => NotificationPreferenceCategories.EventUpdates,
        _ => null
    };
}
