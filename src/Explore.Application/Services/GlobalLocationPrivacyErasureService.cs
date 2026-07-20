// ABOUTME: Coordinates retained-authority-first global account erasure and sequence-ordered replay.
// ABOUTME: Keeps every application mutation, checkpoint, and PII-free correction message in one transaction.

using Explore.Application.Caching;
using Explore.Application.Contracts.LocationPrivacy;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Exceptions;
using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace Explore.Application.Services;

public sealed class GlobalLocationPrivacyErasureService(
    IUserRepository userRepository,
    IGenericRepository<UserPii, Guid> userPiiRepository,
    IUserAuthenticationTokenRepository userAuthenticationTokenRepository,
    IGlobalLocationPrivacyErasureRepository erasureRepository,
    ILocationPrivacyErasureReplayCheckpointRepository checkpointRepository,
    IOutboxRepository outboxRepository,
    ILocationPrivacyErasureAuthority authority,
    IUnitOfWork unitOfWork,
    HybridCache cache,
    TimeProvider timeProvider,
    ILogger<GlobalLocationPrivacyErasureService> logger)
    : IGlobalLocationPrivacyErasureService
{
    private const int ReplayBatchSize = 100;

    public async Task EraseUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        RequireId(userId, nameof(userId));
        cancellationToken.ThrowIfCancellationRequested();
        User? user = await userRepository.GetById(userId);
        if (user is null)
        {
            throw new NotFoundException(nameof(User), userId);
        }

        IReadOnlyList<Location> homes = await erasureRepository.GetOwnedPrivateHomesAsync(
            userId,
            cancellationToken);
        var request = new LocationPrivacyErasureIntent(
            Guid.CreateVersion7(),
            userId,
            homes.Select(home => home.Id).ToArray(),
            LocationPrivacyErasureReasonEnum.AccountDeletion);

        LocationPrivacyErasureAuthorityIntent retained =
            await AppendWithAmbiguousAcknowledgementRetryAsync(request, cancellationToken);
        await ReplayPendingAsync(cancellationToken);

        LocationPrivacyErasureReplayCheckpoint? latest =
            await checkpointRepository.GetLatestAsync(cancellationToken);
        if (latest?.Matches(retained) != true)
        {
            throw new InvalidOperationException(
                "The retained erasure intent was not durably applied to the application database.");
        }
    }

    public async Task ReplayPendingAsync(CancellationToken cancellationToken)
    {
        LocationPrivacyErasureReplayCheckpoint? latest =
            await checkpointRepository.GetLatestAsync(cancellationToken);
        if (latest is not null)
        {
            IReadOnlyList<LocationPrivacyErasureAuthorityIntent> checkpointEvidence =
                await authority.ReadAfterAsync(
                    latest.AuthoritySequence - 1,
                    1,
                    cancellationToken);
            if (checkpointEvidence.Count != 1 || !latest.Matches(checkpointEvidence[0]))
            {
                throw new InvalidOperationException(
                    "The application erasure checkpoint is not continuous with the retained authority.");
            }

            await InvalidateRetainedIntentAsync(checkpointEvidence[0], cancellationToken);
        }

        long afterSequence = latest?.AuthoritySequence ?? 0;
        while (true)
        {
            IReadOnlyList<LocationPrivacyErasureAuthorityIntent> pending =
                await authority.ReadAfterAsync(afterSequence, ReplayBatchSize, cancellationToken);
            if (pending.Count == 0)
            {
                return;
            }

            foreach (LocationPrivacyErasureAuthorityIntent intent in pending)
            {
                await ApplyAsync(intent, cancellationToken);
                afterSequence = intent.AuthoritySequence;
            }
        }
    }

    private async Task ApplyAsync(
        LocationPrivacyErasureAuthorityIntent intent,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<Location> previewHomes = await erasureRepository.GetOwnedPrivateHomesAsync(
            intent.OwnerUserId,
            cancellationToken);
        Guid[] previewLocationIds = previewHomes.Select(home => home.Id).ToArray();
        IReadOnlyList<EventLocation> previewEventLocations =
            await erasureRepository.GetEventLocationsAsync(previewLocationIds, cancellationToken);
        Dictionary<Guid, Guid> locationMessageIds = previewHomes.ToDictionary(
            home => home.Id,
            _ => Guid.CreateVersion7());
        Dictionary<Guid, Guid> correctionMessageIds = previewEventLocations.ToDictionary(
            eventLocation => eventLocation.Id,
            _ => Guid.CreateVersion7());
        Guid checkpointId = Guid.CreateVersion7();
        DateTime appliedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
        if (appliedAtUtc < intent.RecordedAtUtc)
        {
            appliedAtUtc = intent.RecordedAtUtc;
        }

        AppliedErasure applied = await unitOfWork.ExecuteSerializableAsync(async ct =>
        {
            LocationPrivacyErasureReplayCheckpoint? current =
                await checkpointRepository.GetLatestAsync(ct);
            if (current?.Matches(intent) == true)
            {
                return AppliedErasure.None;
            }

            LocationPrivacyErasureReplayCheckpoint checkpoint = current is null
                ? LocationPrivacyErasureReplayCheckpoint.Start(
                    intent,
                    appliedAtUtc,
                    checkpointId)
                : LocationPrivacyErasureReplayCheckpoint.Advance(
                    current,
                    intent,
                    appliedAtUtc,
                    checkpointId);

            IReadOnlyList<Location> homes = await erasureRepository.GetOwnedPrivateHomesAsync(
                intent.OwnerUserId,
                ct);
            Guid[] locationIds = homes.Select(home => home.Id).ToArray();
            IReadOnlyList<EventLocation> eventLocations =
                await erasureRepository.GetEventLocationsAsync(locationIds, ct);
            IReadOnlyList<Actor> actors = await erasureRepository.GetUserActorsAsync(
                intent.OwnerUserId,
                ct);

            var audits = new List<EventLocationDisclosureAudit>(eventLocations.Count);
            foreach (EventLocation eventLocation in eventLocations)
            {
                audits.Add(eventLocation.ChangeDisclosurePolicy(
                    EventLocationDisclosureFields.None,
                    LocationDisclosureAudienceEnum.Never,
                    null,
                    eventLocation.PolicyVersion,
                    intent.OwnerUserId,
                    EventLocationDisclosureAuditReasonEnum.PrivacyErasureRemediation,
                    appliedAtUtc,
                    needsPrivacyReview: true));
            }

            foreach (Location home in homes)
            {
                home.EraseOwnedPii(appliedAtUtc, intent.Reason);
            }

            foreach (Actor actor in actors)
            {
                if (actor.Pii is null)
                {
                    continue;
                }

                actor.Pii.DisplayName = $"DeletedUser{intent.IntentId:N}";
                actor.Pii.Did = null;
                actor.Pii.Handle = null;
                actor.Pii.ProfilePictureUri = null;
            }

            await erasureRepository.SaveChangesAsync(audits, ct);

            User? user = await userRepository.GetById(intent.OwnerUserId);
            UserPii? userPii = await userPiiRepository.GetById(intent.OwnerUserId);
            if (userPii is not null)
            {
                await userPiiRepository.Delete(userPii);
            }

            IReadOnlyList<UserAuthenticationToken> tokens =
                await userAuthenticationTokenRepository.GetByUser(intent.OwnerUserId, ct);
            foreach (UserAuthenticationToken token in tokens)
            {
                await userAuthenticationTokenRepository.Delete(token);
            }

            if (user is not null)
            {
                user.IsDeleted = true;
                user.DeletedAt = appliedAtUtc;
                user.DeletedBy = intent.OwnerUserId;
                await userRepository.Update(user);
            }

            await checkpointRepository.AppendAsync(checkpoint, ct);

            var messages = new List<OutboxMessage>(homes.Count + eventLocations.Count);
            messages.AddRange(homes.Select(home =>
                LocationPrivacyOutboxMessageFactory.CreateLocationErased(
                    locationMessageIds.TryGetValue(home.Id, out Guid messageId)
                        ? messageId
                        : throw new InvalidOperationException(
                            "The owned Home set changed while applying the retained erasure intent."),
                    intent,
                    home,
                    appliedAtUtc)));
            messages.AddRange(eventLocations.Select(eventLocation =>
                LocationPrivacyOutboxMessageFactory.CreateCorrectionRequested(
                    correctionMessageIds.TryGetValue(eventLocation.Id, out Guid messageId)
                        ? messageId
                        : throw new InvalidOperationException(
                            "The EventLocation correction set changed while applying the retained erasure intent."),
                    intent,
                    eventLocation,
                    appliedAtUtc)));
            await outboxRepository.CreateRange(messages, ct);

            return new AppliedErasure(
                intent.OwnerUserId,
                homes.Select(home => home.TenantId).Distinct().ToArray(),
                eventLocations
                    .Select(eventLocation => new CorrectedEventLocation(
                        eventLocation.TenantId,
                        eventLocation.EventId,
                        eventLocation.Id))
                    .ToArray());
        }, cancellationToken);

        await InvalidateAfterCommitAsync(applied);
    }

    private async Task InvalidateAfterCommitAsync(AppliedErasure applied)
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
            throw new InvalidOperationException(
                "Post-commit privacy-erasure cache invalidation failed.");
        }
    }

    private async Task InvalidateRetainedIntentAsync(
        LocationPrivacyErasureAuthorityIntent intent,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<EventLocation> eventLocations =
            await erasureRepository.GetEventLocationsAsync(intent.LocationIds, cancellationToken);
        var applied = new AppliedErasure(
            intent.OwnerUserId,
            eventLocations.Select(eventLocation => eventLocation.TenantId).Distinct().ToArray(),
            eventLocations
                .Select(eventLocation => new CorrectedEventLocation(
                    eventLocation.TenantId,
                    eventLocation.EventId,
                    eventLocation.Id))
                .ToArray());

        await InvalidateAfterCommitAsync(applied);
    }

    private async Task<LocationPrivacyErasureAuthorityIntent>
        AppendWithAmbiguousAcknowledgementRetryAsync(
            LocationPrivacyErasureIntent intent,
            CancellationToken cancellationToken)
    {
        try
        {
            return await authority.AppendAsync(intent, cancellationToken);
        }
        catch (Exception exception) when (
            !cancellationToken.IsCancellationRequested
            && exception is TimeoutException or IOException or InvalidOperationException
                or OperationCanceledException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await authority.AppendAsync(intent, cancellationToken);
        }
    }

    private static void RequireId(Guid id, string parameterName)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("A non-empty id is required.", parameterName);
        }
    }

    private sealed record AppliedErasure(
        Guid UserId,
        IReadOnlyList<Guid> TenantIds,
        IReadOnlyList<CorrectedEventLocation> CorrectedEventLocations)
    {
        public static AppliedErasure None { get; } = new(Guid.Empty, [], []);
    }

    private sealed record CorrectedEventLocation(
        Guid TenantId,
        Guid EventId,
        Guid EventLocationId);
}
