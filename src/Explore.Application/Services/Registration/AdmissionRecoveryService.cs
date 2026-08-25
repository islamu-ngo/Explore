// ABOUTME: Orchestrates uniform recovery requests, single-use consumption, and resend rotation.
// ABOUTME: Commits digest-only state before plaintext crosses the capability-to-delivery seam.

using System.Collections.Concurrent;
using Explore.Application.Contracts.Admissions;
using Explore.Application.Contracts.Persistence;

namespace Explore.Application.Services.Registration;

public sealed class AdmissionRecoveryService
{
    private readonly IAdmissionRecoveryRepository repository;
    private readonly IAdmissionRecoveryCapabilityService capabilityService;
    private readonly IAdmissionRecoveryDeliveryService deliveryService;
    private readonly IUnitOfWork unitOfWork;
    private readonly TimeProvider timeProvider;
    private readonly IAdmissionRecoveryDeliveryStager? deliveryStager;
    private readonly IAdmissionRecoveryTicketDocumentService? ticketDocumentService;
    private readonly IAdmissionRecoveryAuditService? auditService;
    private readonly ConcurrentDictionary<Guid, RecoveryLineage> knownLineage = [];

    public AdmissionRecoveryService(
        IAdmissionRecoveryRepository repository,
        IAdmissionRecoveryCapabilityService capabilityService,
        IAdmissionRecoveryDeliveryService deliveryService,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
        : this(repository, capabilityService, deliveryService, unitOfWork, timeProvider, null, null, null)
    {
    }

    public AdmissionRecoveryService(
        IAdmissionRecoveryRepository repository,
        IAdmissionRecoveryCapabilityService capabilityService,
        IAdmissionRecoveryDeliveryService deliveryService,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        IAdmissionRecoveryDeliveryStager deliveryStager)
        : this(repository, capabilityService, deliveryService, unitOfWork, timeProvider, deliveryStager, null, null)
    {
    }

    public AdmissionRecoveryService(
        IAdmissionRecoveryRepository repository,
        IAdmissionRecoveryCapabilityService capabilityService,
        IAdmissionRecoveryDeliveryService deliveryService,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        IAdmissionRecoveryDeliveryStager deliveryStager,
        IAdmissionRecoveryTicketDocumentService ticketDocumentService)
        : this(
            repository,
            capabilityService,
            deliveryService,
            unitOfWork,
            timeProvider,
            deliveryStager,
            ticketDocumentService,
            null)
    {
    }

    public AdmissionRecoveryService(
        IAdmissionRecoveryRepository repository,
        IAdmissionRecoveryCapabilityService capabilityService,
        IAdmissionRecoveryDeliveryService deliveryService,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        IAdmissionRecoveryDeliveryStager deliveryStager,
        IAdmissionRecoveryTicketDocumentService ticketDocumentService,
        IAdmissionRecoveryAuditService auditService)
    {
        this.repository = repository;
        this.capabilityService = capabilityService;
        this.deliveryService = deliveryService;
        this.unitOfWork = unitOfWork;
        this.timeProvider = timeProvider;
        this.deliveryStager = deliveryStager;
        this.ticketDocumentService = ticketDocumentService;
        this.auditService = auditService;
    }

    public async Task<AdmissionRecoveryRequestResult> RequestAsync(
        AdmissionRecoveryRequest request,
        CancellationToken cancellationToken)
    {
        AdmissionRecoveryRequest normalized = request with
        {
            NormalizedIdentity = NormalizeIdentity(request.NormalizedIdentity)
        };
        AdmissionRecoveryIdentityResult identity =
            await repository.FindIdentityAsync(normalized, cancellationToken);
        if (!identity.IdentityPresent || identity.AdmissionTicketIds.Count == 0)
        {
            return Accepted();
        }

        RecoveryLineage[] lineages = identity.AdmissionTicketIds
            .Select(admissionTicketId => new RecoveryLineage(
                request.TenantId,
                identity.RecoveryRequestId,
                admissionTicketId,
                request.Purpose,
                0))
            .ToArray();
        var materials = new List<(RecoveryLineage Lineage, AdmissionRecoveryCapabilityMaterial Material)>(
            lineages.Length);
        DateTimeOffset createdAtUtc = timeProvider.GetUtcNow();
        foreach (RecoveryLineage lineage in lineages)
        {
            AdmissionRecoveryCapabilityMaterial material = await capabilityService.IssueAsync(
                new AdmissionRecoveryCapabilityIssueRequest(
                    lineage.TenantId,
                    lineage.RecoveryRequestId,
                    lineage.AdmissionTicketId,
                    lineage.Purpose,
                    lineage.KeyVersion),
                cancellationToken);
            materials.Add((lineage, material));
        }

        await unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                foreach ((RecoveryLineage lineage, AdmissionRecoveryCapabilityMaterial material) in materials)
                {
                    await repository.StoreAsync(
                        new AdmissionRecoveryCapabilityRecord(
                            lineage.TenantId,
                            lineage.RecoveryRequestId,
                            lineage.AdmissionTicketId,
                            lineage.Purpose,
                            material.LookupDigest,
                            material.KeyVersion,
                            material.ExpiresAtUtc,
                            Guid.CreateVersion7(),
                            1,
                            createdAtUtc,
                            material.LocatorDigest),
                        token);
                    if (deliveryStager is not null)
                    {
                        await deliveryStager.StageAsync(
                            new AdmissionRecoveryDeliveryRequest(
                                lineage.TenantId,
                                lineage.RecoveryRequestId,
                                lineage.AdmissionTicketId,
                                lineage.Purpose,
                                material.Capability),
                            token);
                    }
                    if (auditService is not null)
                    {
                        await auditService.AppendAsync(
                            new AdmissionRecoveryAuditFact(
                                lineage.TenantId,
                                lineage.RecoveryRequestId,
                                "AdmissionRecoveryIssued",
                                1,
                                createdAtUtc),
                            token);
                    }
                }
            },
            cancellationToken);

        foreach ((RecoveryLineage lineage, AdmissionRecoveryCapabilityMaterial material) in materials)
        {
            knownLineage[lineage.RecoveryRequestId] = lineage with { KeyVersion = material.KeyVersion };
            if (deliveryStager is null)
            {
                await deliveryService.DeliverAsync(
                    new AdmissionRecoveryDeliveryRequest(
                        lineage.TenantId,
                        lineage.RecoveryRequestId,
                        lineage.AdmissionTicketId,
                        lineage.Purpose,
                        material.Capability),
                    cancellationToken);
            }
        }

        return Accepted();
    }

    public async Task<AdmissionRecoveryConsumeResult> ConsumeAsync(
        AdmissionRecoveryConsumeRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Purpose != AdmissionRecoveryPurpose.TicketRecovery)
        {
            return new AdmissionRecoveryConsumeResult(AdmissionRecoveryConsumeOutcome.WrongPurpose);
        }

        RecoveryLineage? lineage = await ResolveLineageAsync(request, cancellationToken);
        if (lineage is null || lineage.TenantId != request.TenantId)
        {
            return new AdmissionRecoveryConsumeResult(AdmissionRecoveryConsumeOutcome.WrongTenant);
        }

        if (lineage.Purpose != request.Purpose)
        {
            return new AdmissionRecoveryConsumeResult(AdmissionRecoveryConsumeOutcome.WrongPurpose);
        }

        AdmissionRecoveryCapabilityDigest digest = await capabilityService.DigestAsync(
            new AdmissionRecoveryCapabilityDigestRequest(
                lineage.TenantId,
                lineage.RecoveryRequestId,
                lineage.AdmissionTicketId,
                lineage.Purpose,
                request.Capability,
                lineage.KeyVersion),
            cancellationToken);
        AdmissionRecoveryCapabilityState state = await repository.GetByDigestAsync(
            new AdmissionRecoveryCapabilityLookup(
                lineage.TenantId,
                lineage.RecoveryRequestId,
                lineage.AdmissionTicketId,
                lineage.Purpose,
                digest.LookupDigest,
                digest.KeyVersion),
            cancellationToken);
        if (!state.Found)
        {
            return new AdmissionRecoveryConsumeResult(AdmissionRecoveryConsumeOutcome.Invalid);
        }

        if (state.Rotated)
        {
            return new AdmissionRecoveryConsumeResult(AdmissionRecoveryConsumeOutcome.Rotated);
        }

        if (state.Consumed)
        {
            return new AdmissionRecoveryConsumeResult(AdmissionRecoveryConsumeOutcome.AlreadyConsumed);
        }

        if (state.ExpiresAtUtc <= timeProvider.GetUtcNow())
        {
            return new AdmissionRecoveryConsumeResult(AdmissionRecoveryConsumeOutcome.Expired);
        }

        AdmissionRecoveryMutationResult mutation = await repository.ConsumeAsync(
            new AdmissionRecoveryCapabilityMutation(
                state.TenantId,
                state.RecoveryRequestId,
                state.AdmissionTicketId,
                state.Purpose,
                state.LookupDigest,
                state.ExpiresAtUtc,
                state.KeyVersion,
                state.CapabilityId,
                state.ConcurrencyStamp,
                timeProvider.GetUtcNow()),
            cancellationToken);
        return mutation.Outcome == AdmissionRecoveryMutationOutcome.Consumed
            ? new AdmissionRecoveryConsumeResult(AdmissionRecoveryConsumeOutcome.Consumed)
            : new AdmissionRecoveryConsumeResult(AdmissionRecoveryConsumeOutcome.AlreadyConsumed);
    }

    public async Task<AdmissionRecoveryConsumeResult> ConsumeByCapabilityAsync(
        Guid tenantId,
        string capability,
        CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty || string.IsNullOrWhiteSpace(capability))
        {
            return new AdmissionRecoveryConsumeResult(AdmissionRecoveryConsumeOutcome.Invalid);
        }

        IReadOnlyList<AdmissionRecoveryLocatorDigest> locators;
        try
        {
            locators = await capabilityService.DigestLocatorsAsync(capability, cancellationToken);
        }
        catch (ArgumentException)
        {
            return new AdmissionRecoveryConsumeResult(AdmissionRecoveryConsumeOutcome.Invalid);
        }
        catch (InvalidOperationException)
        {
            return new AdmissionRecoveryConsumeResult(AdmissionRecoveryConsumeOutcome.Invalid);
        }

        AdmissionRecoveryCapabilityState state = await repository.GetByLocatorAsync(
            tenantId,
            locators,
            cancellationToken);
        if (!state.Found || state.Purpose != AdmissionRecoveryPurpose.TicketRecovery)
        {
            return new AdmissionRecoveryConsumeResult(AdmissionRecoveryConsumeOutcome.Invalid);
        }

        AdmissionRecoveryCapabilityDigest proof = await capabilityService.DigestAsync(
            new AdmissionRecoveryCapabilityDigestRequest(
                state.TenantId,
                state.RecoveryRequestId,
                state.AdmissionTicketId,
                state.Purpose,
                capability,
                state.KeyVersion),
            cancellationToken);
        AdmissionRecoveryCapabilityState verified = await repository.GetByDigestAsync(
            new AdmissionRecoveryCapabilityLookup(
                state.TenantId,
                state.RecoveryRequestId,
                state.AdmissionTicketId,
                state.Purpose,
                proof.LookupDigest,
                proof.KeyVersion),
            cancellationToken);
        if (!verified.Found || verified.Rotated || verified.Consumed ||
            verified.ExpiresAtUtc <= timeProvider.GetUtcNow())
        {
            return new AdmissionRecoveryConsumeResult(AdmissionRecoveryConsumeOutcome.Invalid);
        }

        AdmissionRecoveryConsumeResult result =
            new(AdmissionRecoveryConsumeOutcome.Invalid);
        try
        {
            await unitOfWork.ExecuteInTransactionAsync(
                async token =>
                {
                    AdmissionRecoveryMutationResult mutation = await repository.ConsumeAsync(
                        new AdmissionRecoveryCapabilityMutation(
                            verified.TenantId,
                            verified.RecoveryRequestId,
                            verified.AdmissionTicketId,
                            verified.Purpose,
                            verified.LookupDigest,
                            verified.ExpiresAtUtc,
                            verified.KeyVersion,
                            verified.CapabilityId,
                            verified.ConcurrencyStamp,
                            timeProvider.GetUtcNow()),
                        token);
                    if (mutation.Outcome != AdmissionRecoveryMutationOutcome.Consumed)
                    {
                        return;
                    }

                    AdmissionRecoveryTicketDocument? document = ticketDocumentService is null
                        ? null
                        : await ticketDocumentService.RotateAndCreateAsync(
                            verified.TenantId,
                            verified.AdmissionTicketId,
                            token);
                    if (ticketDocumentService is not null && document is null)
                    {
                        throw new RecoveryDocumentUnavailableException();
                    }

                    result = new AdmissionRecoveryConsumeResult(
                        AdmissionRecoveryConsumeOutcome.Consumed,
                        verified.CapabilityId,
                        document);
                    if (auditService is not null)
                    {
                        await auditService.AppendAsync(
                            new AdmissionRecoveryAuditFact(
                                verified.TenantId,
                                verified.RecoveryRequestId,
                                "AdmissionRecoveryConsumed",
                                verified.CapabilityVersion,
                                timeProvider.GetUtcNow()),
                            token);
                    }
                },
                cancellationToken);
        }
        catch (RecoveryDocumentUnavailableException)
        {
            return new AdmissionRecoveryConsumeResult(AdmissionRecoveryConsumeOutcome.Invalid);
        }

        return result;
    }

    public async Task<AdmissionRecoveryResendResult> ResendAsync(
        AdmissionRecoveryResendRequest request,
        CancellationToken cancellationToken)
    {
        AdmissionRecoveryCapabilityState current = await repository.GetCurrentByRequestIdAsync(
            request.TenantId,
            request.RecoveryRequestId,
            request.Purpose,
            cancellationToken);
        if (!current.Found || current.Consumed || current.Rotated ||
            request.Purpose != AdmissionRecoveryPurpose.TicketRecovery)
        {
            return new AdmissionRecoveryResendResult(AdmissionRecoveryRequestOutcome.Accepted);
        }

        AdmissionRecoveryCapabilityMaterial replacement = await capabilityService.IssueAsync(
            new AdmissionRecoveryCapabilityIssueRequest(
                current.TenantId,
                current.RecoveryRequestId,
                current.AdmissionTicketId,
                current.Purpose,
                current.KeyVersion),
            cancellationToken);
        DateTimeOffset rotatedAtUtc = timeProvider.GetUtcNow();

        AdmissionRecoveryMutationResult rotation = new(AdmissionRecoveryMutationOutcome.Rejected);
        await unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                rotation = await repository.RotateAsync(
                    new AdmissionRecoveryRotationRequest(
                        current.TenantId,
                        current.RecoveryRequestId,
                        current.AdmissionTicketId,
                        current.Purpose,
                        current.LookupDigest,
                        replacement.LookupDigest,
                        replacement.KeyVersion,
                        replacement.ExpiresAtUtc,
                        current.KeyVersion,
                        current.CapabilityId,
                        Guid.CreateVersion7(),
                        current.CapabilityVersion + 1,
                        current.ConcurrencyStamp,
                        rotatedAtUtc,
                        replacement.LocatorDigest),
                    token);
                if (rotation.Outcome == AdmissionRecoveryMutationOutcome.Rotated &&
                    deliveryStager is not null)
                {
                    await deliveryStager.StageAsync(
                        new AdmissionRecoveryDeliveryRequest(
                            current.TenantId,
                            current.RecoveryRequestId,
                            current.AdmissionTicketId,
                            current.Purpose,
                            replacement.Capability,
                            current.CapabilityVersion + 1),
                        token);
                }
                if (rotation.Outcome == AdmissionRecoveryMutationOutcome.Rotated &&
                    auditService is not null)
                {
                    await auditService.AppendAsync(
                        new AdmissionRecoveryAuditFact(
                            current.TenantId,
                            current.RecoveryRequestId,
                            "AdmissionRecoveryRotated",
                            current.CapabilityVersion + 1,
                            rotatedAtUtc),
                        token);
                }
            },
            cancellationToken);
        if (rotation.Outcome != AdmissionRecoveryMutationOutcome.Rotated)
        {
            return new AdmissionRecoveryResendResult(AdmissionRecoveryRequestOutcome.Accepted);
        }

        knownLineage[current.RecoveryRequestId] = new RecoveryLineage(
            current.TenantId,
            current.RecoveryRequestId,
            current.AdmissionTicketId,
            current.Purpose,
            replacement.KeyVersion);
        if (deliveryStager is null)
        {
            await deliveryService.DeliverAsync(
                new AdmissionRecoveryDeliveryRequest(
                    current.TenantId,
                    current.RecoveryRequestId,
                    current.AdmissionTicketId,
                    current.Purpose,
                    replacement.Capability,
                    current.CapabilityVersion + 1),
                cancellationToken);
        }
        return new AdmissionRecoveryResendResult(AdmissionRecoveryRequestOutcome.Accepted);
    }

    private async Task<RecoveryLineage?> ResolveLineageAsync(
        AdmissionRecoveryConsumeRequest request,
        CancellationToken cancellationToken)
    {
        if (knownLineage.TryGetValue(request.RecoveryRequestId, out RecoveryLineage? known))
        {
            return known;
        }

        try
        {
            AdmissionRecoveryCapabilityState current = await repository.GetCurrentByRequestIdAsync(
                request.TenantId,
                request.RecoveryRequestId,
                request.Purpose,
                cancellationToken);
            if (!current.Found)
            {
                return null;
            }

            var resolved = new RecoveryLineage(
                current.TenantId,
                current.RecoveryRequestId,
                current.AdmissionTicketId,
                current.Purpose,
                current.KeyVersion);
            knownLineage.TryAdd(resolved.RecoveryRequestId, resolved);
            return resolved;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private static AdmissionRecoveryRequestResult Accepted() =>
        new(AdmissionRecoveryRequestOutcome.Accepted);

    private static string NormalizeIdentity(string value) =>
        value.Trim().ToUpperInvariant();

    private sealed record RecoveryLineage(
        Guid TenantId,
        Guid RecoveryRequestId,
        Guid AdmissionTicketId,
        AdmissionRecoveryPurpose Purpose,
        int KeyVersion);

    private sealed class RecoveryDocumentUnavailableException : Exception
    {
    }
}
