// ABOUTME: Executes tenant-bound Setup live enrollment authority and value-free readiness reads.
// ABOUTME: Keeps capabilities ephemeral, persists only digests, and reauthorizes every operation.

namespace Explore.Application.Features.SetupLive;

using System.Security.Cryptography;
using System.Text;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Secrets;
using Explore.Application.Contracts.SetupLive;
using Explore.Domain.SetupLive;
using ISLAMU.Wire.Contracts.SetupLive;
using Microsoft.Extensions.Logging;
using DomainEnrollmentState = Explore.Domain.SetupLive.SetupEnrollmentState;
using WireEnrollmentState = ISLAMU.Wire.Contracts.SetupLive.SetupEnrollmentState;

public enum SetupLiveApplicationStatus
{
    Invalid = 0,
    Success = 1,
    Created = 2,
    Duplicate = 3,
    Forbidden = 4,
    Unavailable = 5,
    Conflict = 6
}

public sealed record SetupLiveEnrollmentResult(
    SetupLiveApplicationStatus Status,
    SetupTargetEnrollmentData? Data = null,
    SetupEnrollmentCapability? Capability = null,
    bool CanMutate = false);

public sealed record SetupLiveReadinessResult(
    SetupLiveApplicationStatus Status,
    IReadOnlyList<SetupSecretBindingReadinessItem>? Items = null);

public sealed record SetupLiveSecretBindingResult(
    SetupLiveApplicationStatus Status,
    SetupSecretBindingOperationData? Data = null);

public sealed class SetupLiveApplicationService(
    ISetupLiveRepository setupLiveRepository,
    ISecretBindingRepository secretBindingRepository,
    IUnitOfWork unitOfWork,
    IActorRepository actorRepository,
    IAuthorizationProvider authorization,
    ITenantContext tenantContext,
    ISetupSecretBindingWriter secretBindingWriter,
    ISetupSecretBindingCommitmentAuthority commitmentAuthority,
    ISetupSecretBindingOperationCoordinator operationCoordinator,
    ISetupSecretBindingCommitBarrier commitBarrier,
    TimeProvider timeProvider,
    ILogger<SetupLiveApplicationService> logger)
{
    private static readonly EventId MilestoneEvent = new(19_620, "SetupLiveMilestone");
    private static readonly TimeSpan EnrollmentLifetime = TimeSpan.FromMinutes(15);

    public Task<SetupLiveSecretBindingResult> WriteSecretBindingAsync(
        Guid tenantId,
        Guid enrollmentId,
        Guid userId,
        string? capability,
        Guid operationKey,
        string bindingKey,
        ReadOnlyMemory<byte> secretValue,
        CancellationToken cancellationToken) =>
        Task.FromResult(new SetupLiveSecretBindingResult(
            SetupLiveApplicationStatus.Unavailable));

    public async Task<SetupLiveEnrollmentResult> CreateAsync(
        Guid tenantId,
        Guid userId,
        Guid operationKey,
        CreateSetupTargetEnrollmentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!IsVersion7(tenantId)
            || tenantContext.TenantId != tenantId
            || !IsVersion7(userId)
            || !IsVersion7(operationKey))
        {
            return new(SetupLiveApplicationStatus.Invalid);
        }

        if (!await IsAuthorizedAsync(
                tenantId,
                userId,
                AuthorizationActions.Tenants.Update,
                cancellationToken))
        {
            return new(SetupLiveApplicationStatus.Forbidden);
        }

        Explore.Domain.Actor? actor = await actorRepository.GetActorByUserIdAndTenantId(
            userId,
            tenantId,
            cancellationToken);
        if (actor is null || !IsVersion7(actor.Id))
            return new(SetupLiveApplicationStatus.Forbidden);

        string challenge = request.ClientChallenge.ToWireValue();
        string scopeDigest = ScopeDigest(request.RequestedScopes);
        string requestFingerprint = Digest(
            $"setup-live-enrollment-v1\n{tenantId:D}\n{actor.Id:D}\n{challenge}\n{scopeDigest}");
        DateTime createdAt = UtcNow();
        Guid enrollmentId = Guid.CreateVersion7();
        Guid claimId = Guid.CreateVersion7();
        byte[]? capabilityBytes = null;
        try
        {
            return await unitOfWork.ExecuteSerializableAsync(
                async token =>
                {
                    SetupEnrollmentIssuanceClaim? existingClaim =
                        await setupLiveRepository.FindIssuanceClaimAsync(
                            tenantId,
                            operationKey,
                            token);
                    if (existingClaim is not null)
                    {
                        if (existingClaim.Match(
                                tenantId,
                                actor.Id,
                                requestFingerprint)
                            == SetupReplayDecision.Conflict)
                        {
                            return new SetupLiveEnrollmentResult(
                                SetupLiveApplicationStatus.Conflict);
                        }

                        SetupTargetEnrollment? existingEnrollment =
                            await setupLiveRepository.FindEnrollmentAsync(
                                tenantId,
                                existingClaim.EnrollmentId,
                                token);
                        if (existingEnrollment is null
                            || !existingEnrollment.IsAvailable(
                                tenantId,
                                actor.Id,
                                existingClaim.EnrollmentGeneration,
                                UtcNow())
                            || !FixedTimeEquals(
                                existingEnrollment.ScopeDigest,
                                scopeDigest))
                        {
                            return new SetupLiveEnrollmentResult(
                                SetupLiveApplicationStatus.Unavailable);
                        }

                        return new SetupLiveEnrollmentResult(
                            SetupLiveApplicationStatus.Duplicate,
                            MapEnrollment(
                                existingEnrollment,
                                SetupEnrollmentIssuance.AlreadyIssued,
                                UtcNow()),
                            CanMutate: true);
                    }

                    if (!await IsAuthorizedAsync(
                            tenantId,
                            userId,
                            AuthorizationActions.Tenants.Update,
                            token))
                    {
                        return new SetupLiveEnrollmentResult(
                            SetupLiveApplicationStatus.Forbidden);
                    }

                    capabilityBytes ??= RandomNumberGenerator.GetBytes(
                        SetupEnrollmentCapability.ByteLength);
                    SetupEnrollmentCapability capability =
                        SetupEnrollmentCapability.FromBytes(capabilityBytes);
                    var enrollment = SetupTargetEnrollment.Create(
                        enrollmentId,
                        tenantId,
                        actor.Id,
                        Digest(challenge),
                        Digest(capability.ToHeaderValue()),
                        scopeDigest,
                        createdAt,
                        createdAt.Add(EnrollmentLifetime));
                    var claim = SetupEnrollmentIssuanceClaim.Create(
                        claimId,
                        tenantId,
                        actor.Id,
                        operationKey,
                        enrollment.Id,
                        enrollment.Generation,
                        requestFingerprint,
                        createdAt);

                    await setupLiveRepository.AddAsync(enrollment, token);
                    await setupLiveRepository.AddAsync(claim, token);
                    LogMilestone("enrollment.create", "before_commit");
                    token.ThrowIfCancellationRequested();
                    await setupLiveRepository.SaveChangesAsync(token);
                    return new SetupLiveEnrollmentResult(
                        SetupLiveApplicationStatus.Created,
                        MapEnrollment(
                            enrollment,
                            SetupEnrollmentIssuance.Issued,
                            createdAt),
                        capability,
                        CanMutate: true);
                },
                cancellationToken);
        }
        finally
        {
            if (capabilityBytes is not null)
                CryptographicOperations.ZeroMemory(capabilityBytes);
        }
    }

    public async Task<SetupLiveEnrollmentResult> GetAsync(
        Guid tenantId,
        Guid enrollmentId,
        Guid userId,
        string? capability,
        CancellationToken cancellationToken)
    {
        AuthorizedEnrollment? authorized = await AuthorizeEnrollmentAsync(
            tenantId,
            enrollmentId,
            userId,
            capability,
            AuthorizationActions.Tenants.View,
            requiredScope: SetupEnrollmentScope.TargetRead,
            cancellationToken);
        if (authorized is null)
            return new(SetupLiveApplicationStatus.Unavailable);

        bool canMutate = await IsAuthorizedAsync(
            tenantId,
            userId,
            AuthorizationActions.Tenants.Update,
            cancellationToken);
        return new(
            SetupLiveApplicationStatus.Success,
            MapEnrollment(
                authorized.Enrollment,
                SetupEnrollmentIssuance.AlreadyIssued,
                UtcNow()),
            CanMutate: canMutate);
    }

    public async Task<SetupLiveEnrollmentResult> RevokeAsync(
        Guid tenantId,
        Guid enrollmentId,
        Guid userId,
        Guid operationKey,
        string? capability,
        CancellationToken cancellationToken)
    {
        if (!IsVersion7(operationKey))
            return new(SetupLiveApplicationStatus.Unavailable);

        AuthorizedEnrollment? authorized = await AuthorizeEnrollmentAsync(
            tenantId,
            enrollmentId,
            userId,
            capability,
            AuthorizationActions.Tenants.Update,
            requiredScope: SetupEnrollmentScope.TargetRead,
            cancellationToken);
        if (authorized is null)
            return new(SetupLiveApplicationStatus.Unavailable);

        DateTime observedAt = UtcNow();
        Guid claimId = Guid.CreateVersion7();
        string requestFingerprint = Digest(
            $"setup-live-revocation-v1\n{tenantId:D}\n{authorized.Enrollment.ActorId:D}\n{enrollmentId:D}\n{Digest(capability!)}");
        return await unitOfWork.ExecuteSerializableAsync(
            async token =>
            {
                SetupEnrollmentIssuanceClaim? existingClaim =
                    await setupLiveRepository.FindIssuanceClaimAsync(
                        tenantId,
                        operationKey,
                        token);
                if (existingClaim is not null)
                {
                    return existingClaim.Match(
                            tenantId,
                            authorized.Enrollment.ActorId,
                            requestFingerprint)
                        == SetupReplayDecision.Conflict
                        ? new SetupLiveEnrollmentResult(
                            SetupLiveApplicationStatus.Conflict)
                        : new SetupLiveEnrollmentResult(
                            SetupLiveApplicationStatus.Unavailable);
                }

                AuthorizedEnrollment? current = await AuthorizeEnrollmentAsync(
                    tenantId,
                    enrollmentId,
                    userId,
                    capability,
                    AuthorizationActions.Tenants.Update,
                    requiredScope: SetupEnrollmentScope.TargetRead,
                    token);
                if (current is null)
                    return new SetupLiveEnrollmentResult(
                        SetupLiveApplicationStatus.Unavailable);

                var claim = SetupEnrollmentIssuanceClaim.Create(
                    claimId,
                    tenantId,
                    current.Enrollment.ActorId,
                    operationKey,
                    current.Enrollment.Id,
                    current.Enrollment.Generation,
                    requestFingerprint,
                    observedAt);
                current.Enrollment.Revoke(observedAt);
                await setupLiveRepository.AddAsync(claim, token);
                await setupLiveRepository.SaveChangesAsync(token);
                return new SetupLiveEnrollmentResult(
                    SetupLiveApplicationStatus.Success,
                    MapEnrollment(
                        current.Enrollment,
                        SetupEnrollmentIssuance.AlreadyIssued,
                        observedAt));
            },
            cancellationToken);
    }

    public async Task<SetupLiveEnrollmentResult> RotateAsync(
        Guid tenantId,
        Guid enrollmentId,
        Guid userId,
        Guid operationKey,
        string? capability,
        CancellationToken cancellationToken)
    {
        if (!IsVersion7(tenantId)
            || tenantContext.TenantId != tenantId
            || !IsVersion7(enrollmentId)
            || !IsVersion7(userId)
            || !IsVersion7(operationKey)
            || !SetupEnrollmentCapability.TryCreate(
                capability,
                out SetupEnrollmentCapability? currentCapability))
        {
            return new(SetupLiveApplicationStatus.Unavailable);
        }

        Explore.Domain.Actor? actor = await actorRepository.GetActorByUserIdAndTenantId(
            userId,
            tenantId,
            cancellationToken);
        if (actor is null
            || !IsVersion7(actor.Id)
            || !await IsAuthorizedAsync(
                tenantId,
                userId,
                AuthorizationActions.Tenants.Update,
                cancellationToken))
        {
            return new(SetupLiveApplicationStatus.Unavailable);
        }

        string requestFingerprint = Digest(
            $"setup-live-rotation-v1\n{tenantId:D}\n{actor.Id:D}\n{enrollmentId:D}\n{Digest(currentCapability!.ToHeaderValue())}");
        DateTime observedAt = UtcNow();
        Guid claimId = Guid.CreateVersion7();

        byte[]? capabilityBytes = null;
        try
        {
            return await unitOfWork.ExecuteSerializableAsync(
                async token =>
                {
                    if (!await IsAuthorizedAsync(
                            tenantId,
                            userId,
                            AuthorizationActions.Tenants.Update,
                            token))
                    {
                        return new SetupLiveEnrollmentResult(
                            SetupLiveApplicationStatus.Unavailable);
                    }

                    SetupEnrollmentIssuanceClaim? existingClaim =
                        await setupLiveRepository.FindIssuanceClaimAsync(
                            tenantId,
                            operationKey,
                            token);
                    if (existingClaim is not null)
                    {
                        if (existingClaim.Match(
                                tenantId,
                                actor.Id,
                                requestFingerprint)
                            == SetupReplayDecision.Conflict)
                        {
                            return new SetupLiveEnrollmentResult(
                                SetupLiveApplicationStatus.Conflict);
                        }

                        SetupTargetEnrollment? existingEnrollment =
                            await setupLiveRepository.FindEnrollmentAsync(
                                tenantId,
                                existingClaim.EnrollmentId,
                                token);
                        if (existingEnrollment is null
                            || !existingEnrollment.IsAvailable(
                                tenantId,
                                actor.Id,
                                existingClaim.EnrollmentGeneration,
                                UtcNow())
                            || !RecoverScopes(existingEnrollment.ScopeDigest)
                                .Contains(SetupEnrollmentScope.TargetRead))
                        {
                            return new SetupLiveEnrollmentResult(
                                SetupLiveApplicationStatus.Unavailable);
                        }

                        return new SetupLiveEnrollmentResult(
                            SetupLiveApplicationStatus.Duplicate,
                            MapEnrollment(
                                existingEnrollment,
                                SetupEnrollmentIssuance.AlreadyIssued,
                                UtcNow()),
                            CanMutate: true);
                    }

                    AuthorizedEnrollment? authorized =
                        await AuthorizeEnrollmentAsync(
                            tenantId,
                            enrollmentId,
                            userId,
                            currentCapability.ToHeaderValue(),
                            AuthorizationActions.Tenants.Update,
                            requiredScope: SetupEnrollmentScope.TargetRead,
                            token);
                    if (authorized is null)
                        return new(SetupLiveApplicationStatus.Unavailable);

                    capabilityBytes ??= RandomNumberGenerator.GetBytes(
                        SetupEnrollmentCapability.ByteLength);
                    SetupEnrollmentCapability rotated =
                        SetupEnrollmentCapability.FromBytes(capabilityBytes);
                    authorized.Enrollment.RotateCapability(
                        Digest(rotated.ToHeaderValue()),
                        authorized.Enrollment.ExpiresAt.Add(EnrollmentLifetime),
                        observedAt);
                    var claim = SetupEnrollmentIssuanceClaim.Create(
                        claimId,
                        tenantId,
                        actor.Id,
                        operationKey,
                        authorized.Enrollment.Id,
                        authorized.Enrollment.Generation,
                        requestFingerprint,
                        observedAt);
                    await setupLiveRepository.AddAsync(claim, token);
                    await setupLiveRepository.SaveChangesAsync(token);
                    return new SetupLiveEnrollmentResult(
                        SetupLiveApplicationStatus.Success,
                        MapEnrollment(
                            authorized.Enrollment,
                            SetupEnrollmentIssuance.Issued,
                            observedAt),
                        rotated,
                        CanMutate: true);
                },
                cancellationToken);
        }
        finally
        {
            if (capabilityBytes is not null)
                CryptographicOperations.ZeroMemory(capabilityBytes);
        }
    }

    public async Task<SetupLiveReadinessResult> ReadinessAsync(
        Guid tenantId,
        Guid enrollmentId,
        Guid userId,
        string? capability,
        CancellationToken cancellationToken)
    {
        AuthorizedEnrollment? authorized = await AuthorizeEnrollmentAsync(
            tenantId,
            enrollmentId,
            userId,
            capability,
            AuthorizationActions.Tenants.View,
            requiredScope: SetupEnrollmentScope.SecretBindingReadiness,
            cancellationToken);
        if (authorized is null)
            return new(SetupLiveApplicationStatus.Unavailable);

        SetupSecretBindingReadinessItem[] items =
        [
            new()
            {
                BindingKey = "setup.signing",
                State = SetupSecretBindingReadinessState.Unavailable
            },
            new()
            {
                BindingKey = "setup.encryption",
                State = SetupSecretBindingReadinessState.Unavailable
            }
        ];

        return new(SetupLiveApplicationStatus.Success, Array.AsReadOnly(items));
    }

    public async Task<SetupLiveApplicationStatus> ValidateSecretWriteAsync(
        Guid tenantId,
        Guid enrollmentId,
        Guid userId,
        string? capability,
        string bindingKey,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(bindingKey, "setup.signing", StringComparison.Ordinal)
            && !string.Equals(
                bindingKey,
                "setup.encryption",
                StringComparison.Ordinal))
        {
            return SetupLiveApplicationStatus.Unavailable;
        }

        AuthorizedEnrollment? authorized = await AuthorizeEnrollmentAsync(
            tenantId,
            enrollmentId,
            userId,
            capability,
            AuthorizationActions.Tenants.Update,
            requiredScope: SetupEnrollmentScope.SecretBindingWrite,
            cancellationToken);
        return authorized is null
            ? SetupLiveApplicationStatus.Unavailable
            : SetupLiveApplicationStatus.Success;
    }

    private async Task<AuthorizedEnrollment?> AuthorizeEnrollmentAsync(
        Guid tenantId,
        Guid enrollmentId,
        Guid userId,
        string? capabilityValue,
        string action,
        SetupEnrollmentScope requiredScope,
        CancellationToken cancellationToken)
    {
        if (!IsVersion7(tenantId)
            || tenantContext.TenantId != tenantId
            || !IsVersion7(enrollmentId)
            || !IsVersion7(userId))
        {
            return null;
        }

        Explore.Domain.Actor? actor = await actorRepository.GetActorByUserIdAndTenantId(
            userId,
            tenantId,
            cancellationToken);
        if (actor is null
            || !IsVersion7(actor.Id)
            || !await IsAuthorizedAsync(tenantId, userId, action, cancellationToken))
        {
            return null;
        }

        SetupTargetEnrollment? enrollment =
            await setupLiveRepository.FindEnrollmentAsync(
                tenantId,
                enrollmentId,
                cancellationToken);
        if (enrollment is null
            || !SetupEnrollmentCapability.TryCreate(
                capabilityValue,
                out SetupEnrollmentCapability? capability)
            || !CapabilityMatches(enrollment, capability!)
            || !enrollment.IsAvailable(
                tenantId,
                actor.Id,
                enrollment.Generation,
                UtcNow()))
        {
            return null;
        }

        IReadOnlyList<SetupEnrollmentScope> scopes = RecoverScopes(
            enrollment.ScopeDigest);
        return scopes.Contains(requiredScope)
            ? new AuthorizedEnrollment(enrollment, scopes)
            : null;
    }

    private async Task<bool> IsAuthorizedAsync(
        Guid tenantId,
        Guid userId,
        string action,
        CancellationToken cancellationToken)
    {
        AuthorizationDecision decision = await authorization.AuthorizeAsync(
            new AuthorizationRequest(
                ResourceKinds.Tenant,
                tenantId.ToString("D"),
                action,
                new AuthorizationScope(TenantId: tenantId.ToString("D")),
                new TenantScopedAuthorizationFacts(tenantId),
                Subject: new AuthorizationSubject(userId),
                Tenant: new AuthorizationTenant(TenantId: tenantId)),
            cancellationToken);
        return decision.IsAllowed;
    }

    private static SetupTargetEnrollmentData MapEnrollment(
        SetupTargetEnrollment enrollment,
        SetupEnrollmentIssuance issuance,
        DateTime observedAt) => new()
    {
        EnrollmentId = enrollment.Id,
        State = enrollment.State switch
        {
            DomainEnrollmentState.Revoked => WireEnrollmentState.Revoked,
            DomainEnrollmentState.Expired => WireEnrollmentState.Expired,
            _ when observedAt >= enrollment.ExpiresAt => WireEnrollmentState.Expired,
            _ => WireEnrollmentState.Active
        },
        Generation = enrollment.Generation,
        ExpiresAt = new DateTimeOffset(enrollment.ExpiresAt),
        Scopes = RecoverScopes(enrollment.ScopeDigest),
        Issuance = issuance
    };

    private static IReadOnlyList<SetupEnrollmentScope> RecoverScopes(
        string expectedDigest)
    {
        SetupEnrollmentScope[] all = Enum.GetValues<SetupEnrollmentScope>();
        for (int mask = 1; mask < 1 << all.Length; mask++)
        {
            SetupEnrollmentScope[] candidate = all
                .Where((_, index) => (mask & 1 << index) != 0)
                .ToArray();
            if (FixedTimeEquals(ScopeDigest(candidate), expectedDigest))
                return Array.AsReadOnly(candidate);
        }

        throw new InvalidOperationException("Setup enrollment scope evidence is invalid.");
    }

    private static string ScopeDigest(IEnumerable<SetupEnrollmentScope> scopes) =>
        Digest(string.Join(
            '\n',
            scopes.Select(ScopeWireValue).Order(StringComparer.Ordinal)));

    private static string ScopeWireValue(SetupEnrollmentScope scope) => scope switch
    {
        SetupEnrollmentScope.TargetRead => "target.read",
        SetupEnrollmentScope.SecretBindingReadiness => "secret_binding.readiness",
        SetupEnrollmentScope.SecretBindingWrite => "secret_binding.write",
        _ => throw new ArgumentOutOfRangeException(nameof(scope))
    };

    private static bool CapabilityMatches(
        SetupTargetEnrollment enrollment,
        SetupEnrollmentCapability capability) =>
        FixedTimeEquals(
            enrollment.CapabilityDigest,
            Digest(capability.ToHeaderValue()));

    private static bool FixedTimeEquals(string left, string right) =>
        left.Length == right.Length
        && CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(left),
            Encoding.ASCII.GetBytes(right));

    private static string Digest(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))
            .ToLowerInvariant();

    private static bool IsVersion7(Guid value)
    {
        if (value == Guid.Empty || value.Version != 7)
            return false;
        Span<byte> bytes = stackalloc byte[16];
        return value.TryWriteBytes(bytes, bigEndian: true, out int written)
            && written == bytes.Length
            && (bytes[8] & 0b1100_0000) == 0b1000_0000;
    }

    private DateTime UtcNow() => timeProvider.GetUtcNow().UtcDateTime;

    private void LogMilestone(string operation, string milestone) =>
        logger.LogInformation(
            MilestoneEvent,
            "Setup live milestone {SetupOperation} {SetupMilestone}",
            operation,
            milestone);

    private sealed record AuthorizedEnrollment(
        SetupTargetEnrollment Enrollment,
        IReadOnlyList<SetupEnrollmentScope> Scopes);
}
