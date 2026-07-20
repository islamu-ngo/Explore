// ABOUTME: Applies one typed platform privacy-erasure fact inside an existing application transaction.
// ABOUTME: Shares User and location mutation, checkpoint, outbox, mirror, and cache behavior across durability modes.

using Explore.Application.Caching;
using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace Explore.Application.Services;

public sealed class PrivacyErasureApplier(
    IUserRepository userRepository,
    IGenericRepository<UserPii, Guid> userPiiRepository,
    IUserAuthenticationTokenRepository tokenRepository,
    IUserLocationPrivacyErasureRepository erasureRepository,
    IPrivacyErasureReplayCheckpointRepository checkpointRepository,
    IPrivacyErasureLedgerRepository ledgerRepository,
    IOutboxRepository outboxRepository,
    HybridCache cache,
    TimeProvider timeProvider,
    ILogger<PrivacyErasureApplier> logger)
{
    public async Task<PreparedErasure> PrepareAsync(
        PrivacyErasureIntent intent,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<Location> homes = await erasureRepository.GetOwnedPrivateHomesAsync(
            intent.SubjectId,
            cancellationToken);
        IReadOnlyList<EventLocation> eventLocations = await erasureRepository.GetEventLocationsAsync(
            homes.Select(home => home.Id).ToArray(),
            cancellationToken);
        DateTime appliedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
        if (appliedAtUtc < intent.RecordedAtUtc)
        {
            appliedAtUtc = intent.RecordedAtUtc;
        }

        return new PreparedErasure(
            homes.ToDictionary(home => home.Id, _ => Guid.CreateVersion7()),
            eventLocations.ToDictionary(item => item.Id, _ => Guid.CreateVersion7()),
            Guid.CreateVersion7(),
            appliedAtUtc);
    }

    public async Task<AppliedErasure> ApplyInCurrentTransactionAsync(
        PrivacyErasureIntent intent,
        PreparedErasure prepared,
        CancellationToken cancellationToken)
    {
        PrivacyErasureReplayCheckpoint? current =
            await checkpointRepository.GetLatestAsync(cancellationToken);
        if (current?.Matches(intent) == true)
        {
            return AppliedErasure.None;
        }

        await ledgerRepository.AppendAsync(intent, cancellationToken);
        PrivacyErasureReplayCheckpoint checkpoint = current is null
            ? PrivacyErasureReplayCheckpoint.Start(intent, prepared.AppliedAtUtc, prepared.CheckpointId)
            : PrivacyErasureReplayCheckpoint.Advance(current, intent, prepared.AppliedAtUtc, prepared.CheckpointId);
        IReadOnlyList<Location> homes = await erasureRepository.GetOwnedPrivateHomesAsync(
            intent.SubjectId,
            cancellationToken);
        IReadOnlyList<EventLocation> eventLocations = await erasureRepository.GetEventLocationsAsync(
            homes.Select(home => home.Id).ToArray(),
            cancellationToken);
        IReadOnlyList<Actor> actors = await erasureRepository.GetUserActorsAsync(
            intent.SubjectId,
            cancellationToken);

        var audits = new List<EventLocationDisclosureAudit>(eventLocations.Count);
        foreach (EventLocation eventLocation in eventLocations)
        {
            audits.Add(eventLocation.ChangeDisclosurePolicy(
                EventLocationDisclosureFields.None,
                LocationDisclosureAudienceEnum.Never,
                null,
                eventLocation.PolicyVersion,
                intent.SubjectId,
                EventLocationDisclosureAuditReasonEnum.PrivacyErasureRemediation,
                prepared.AppliedAtUtc,
                needsPrivacyReview: true));
        }

        foreach (Location home in homes)
        {
            home.EraseOwnedPii(prepared.AppliedAtUtc, ToLocationReason(intent.ReasonCode));
        }

        foreach (Actor actor in actors.Where(actor => actor.Pii is not null))
        {
            actor.Pii!.DisplayName = $"DeletedUser{intent.IntentId:N}";
            actor.Pii.Did = null;
            actor.Pii.Handle = null;
            actor.Pii.ProfilePictureUri = null;
        }

        await erasureRepository.SaveChangesAsync(audits, cancellationToken);
        User? user = await userRepository.GetById(intent.SubjectId);
        UserPii? userPii = await userPiiRepository.GetById(intent.SubjectId);
        if (userPii is not null)
        {
            await userPiiRepository.Delete(userPii);
        }

        IReadOnlyList<UserAuthenticationToken> tokens =
            await tokenRepository.GetByUser(intent.SubjectId, cancellationToken);
        foreach (UserAuthenticationToken token in tokens)
        {
            await tokenRepository.Delete(token);
        }

        if (user is not null)
        {
            user.IsDeleted = true;
            user.DeletedAt = prepared.AppliedAtUtc;
            user.DeletedBy = intent.SubjectId;
            await userRepository.Update(user);
        }

        await checkpointRepository.AppendAsync(checkpoint, cancellationToken);
        var messages = new List<OutboxMessage>(homes.Count + eventLocations.Count);
        messages.AddRange(homes.Select(home => LocationPrivacyOutboxMessageFactory.CreateLocationErased(
            prepared.LocationMessageIds.TryGetValue(home.Id, out Guid id)
                ? id
                : throw new InvalidOperationException("The owned Home set changed while applying the erasure intent."),
            intent,
            home,
            prepared.AppliedAtUtc)));
        messages.AddRange(eventLocations.Select(eventLocation => LocationPrivacyOutboxMessageFactory.CreateCorrectionRequested(
            prepared.CorrectionMessageIds.TryGetValue(eventLocation.Id, out Guid id)
                ? id
                : throw new InvalidOperationException("The EventLocation correction set changed while applying the erasure intent."),
            intent,
            eventLocation,
            prepared.AppliedAtUtc)));
        await outboxRepository.CreateRange(messages, cancellationToken);

        return new AppliedErasure(
            intent.SubjectId,
            homes.Select(home => home.TenantId).Distinct().ToArray(),
            eventLocations.Select(item => new CorrectedEventLocation(item.TenantId, item.EventId, item.Id)).ToArray());
    }

    public async Task InvalidateAfterCommitAsync(AppliedErasure applied)
    {
        if (applied == AppliedErasure.None)
        {
            return;
        }

        try
        {
            await cache.RemoveAsync($"user:detail:{applied.UserId}", CancellationToken.None);
            await cache.RemoveByTagAsync(CacheTags.EventLocations, CancellationToken.None);
            foreach (Guid tenantId in applied.TenantIds)
            {
                await cache.RemoveByTagAsync(CacheTags.EventLocationsByTenant(tenantId), CancellationToken.None);
                await cache.RemoveByTagAsync(CacheTags.EventListByTenant(tenantId), CancellationToken.None);
            }

            foreach (CorrectedEventLocation corrected in applied.CorrectedEventLocations)
            {
                await cache.RemoveByTagAsync(CacheTags.Event(corrected.EventId), CancellationToken.None);
                await cache.RemoveByTagAsync(CacheTags.EventLocationsByEvent(corrected.EventId), CancellationToken.None);
                await cache.RemoveByTagAsync(CacheTags.EventLocation(corrected.EventLocationId), CancellationToken.None);
            }
        }
        catch (Exception)
        {
            logger.LogWarning("Post-commit privacy-erasure cache invalidation failed.");
            throw new InvalidOperationException("Post-commit privacy-erasure cache invalidation failed.");
        }
    }

    public async Task InvalidateRetainedIntentAsync(
        PrivacyErasureIntent intent,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<Location> homes = await erasureRepository.GetOwnedPrivateHomesAsync(
            intent.SubjectId,
            cancellationToken);
        IReadOnlyList<EventLocation> eventLocations = await erasureRepository.GetEventLocationsAsync(
            homes.Select(home => home.Id).ToArray(),
            cancellationToken);
        await InvalidateAfterCommitAsync(new AppliedErasure(
            intent.SubjectId,
            eventLocations.Select(item => item.TenantId).Distinct().ToArray(),
            eventLocations.Select(item => new CorrectedEventLocation(item.TenantId, item.EventId, item.Id)).ToArray()));
    }

    private static LocationPrivacyErasureReasonEnum ToLocationReason(PrivacyErasureReasonCode reasonCode) =>
        reasonCode switch
        {
            PrivacyErasureReasonCode.AccountDeletion => LocationPrivacyErasureReasonEnum.AccountDeletion,
            PrivacyErasureReasonCode.SubjectErasureRequest => LocationPrivacyErasureReasonEnum.OwnerErasureRequest,
            PrivacyErasureReasonCode.PrivacyIncidentRemediation =>
                LocationPrivacyErasureReasonEnum.PrivacyIncidentRemediation,
            _ => throw new ArgumentOutOfRangeException(nameof(reasonCode))
        };

    public sealed record PreparedErasure(
        IReadOnlyDictionary<Guid, Guid> LocationMessageIds,
        IReadOnlyDictionary<Guid, Guid> CorrectionMessageIds,
        Guid CheckpointId,
        DateTime AppliedAtUtc);

    public sealed record AppliedErasure(
        Guid UserId,
        IReadOnlyList<Guid> TenantIds,
        IReadOnlyList<CorrectedEventLocation> CorrectedEventLocations)
    {
        public static AppliedErasure None { get; } = new(Guid.Empty, [], []);
    }

    public sealed record CorrectedEventLocation(Guid TenantId, Guid EventId, Guid EventLocationId);
}
