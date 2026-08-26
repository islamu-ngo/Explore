// ABOUTME: Verifies bearer-to-entity scanner authentication uses bounded digest candidates and one lookup.
// ABOUTME: Proves singular scope, generic failure, cancellation, and absence of plaintext or digest output.

using Explore.Application.Contracts.Admissions;
using Explore.Application.Services.Registration;
using Explore.Domain;
using NSubstitute;

namespace ApplicationUnitTests.Contracts.Admissions;

public sealed class AdmissionScannerAuthenticationServiceTests
{
    private static readonly DateTimeOffset UtcNow = new(2026, 8, 26, 12, 0, 0, TimeSpan.Zero);

    [Test]
    public async Task ActiveBearerUsesOneGlobalCandidateLookupAndReturnsOneExactTarget()
    {
        AdmissionScannerCapability capability = Capability(UtcNow.AddHours(1));
        var material = Substitute.For<IAdmissionScannerCapabilityMaterialService>();
        AdmissionScannerCapabilityDigestCandidate[] candidates =
        [new(7, "current-digest"), new(6, "retained-digest")];
        material.DigestCandidatesAsync(
                Arg.Any<AdmissionScannerCapabilityDigestCandidatesRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new AdmissionScannerCapabilityDigestCandidates(candidates));
        var repository = Substitute.For<IAdmissionScannerCapabilityRepository>();
        repository.FindByDigestCandidatesAsync(
                Arg.Any<IReadOnlyList<AdmissionScannerCapabilityDigestCandidate>>(),
                Arg.Any<CancellationToken>())
            .Returns(capability);
        var service = new AdmissionScannerAuthenticationService(
            material, repository, new FixedTimeProvider(UtcNow));

        AdmissionScannerAuthenticationResult result = await service.AuthenticateAsync(
            new AdmissionScannerAuthenticationRequest("opaque-scanner-bearer"),
            CancellationToken.None);

        await Assert.That(result.Authenticated).IsTrue();
        await Assert.That(result.ScannerCapabilityId).IsEqualTo(capability.Id);
        await Assert.That(result.TenantId).IsEqualTo(capability.TenantId);
        await Assert.That(result.EventId).IsEqualTo(capability.EventId);
        await Assert.That(result.TargetId).IsEqualTo(capability.AdmissionTargetId);
        await Assert.That(result.Actions).IsEquivalentTo([
            AdmissionCheckInAction.CheckIn, AdmissionCheckInAction.Undo]);
        await repository.Received(1).FindByDigestCandidatesAsync(
            Arg.Is<IReadOnlyList<AdmissionScannerCapabilityDigestCandidate>>(values =>
                values.Count == 2 && values[0].KeyVersion == 7 && values[1].KeyVersion == 6),
            Arg.Any<CancellationToken>());
        await Assert.That(result.GetType().GetProperties().Any(property =>
            property.Name.Contains("Digest", StringComparison.OrdinalIgnoreCase) ||
            property.Name.Contains("Plaintext", StringComparison.OrdinalIgnoreCase) ||
            property.Name.Contains("Bearer", StringComparison.OrdinalIgnoreCase))).IsFalse();
        await Assert.That(result.ToString()).DoesNotContain("opaque-scanner-bearer");
    }

    [Test]
    [Arguments("Missing")]
    [Arguments("Expired")]
    [Arguments("Revoked")]
    public async Task MissingExpiredAndRevokedBearersReturnTheSameGenericFailure(string rejection)
    {
        AdmissionScannerCapability? capability = rejection == "Missing"
            ? null
            : Capability(rejection == "Expired" ? UtcNow : UtcNow.AddHours(1));
        if (rejection == "Revoked")
        {
            capability!.Revoke(Guid.CreateVersion7(), "Compromised", UtcNow.UtcDateTime);
        }
        var material = Substitute.For<IAdmissionScannerCapabilityMaterialService>();
        material.DigestCandidatesAsync(
                Arg.Any<AdmissionScannerCapabilityDigestCandidatesRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new AdmissionScannerCapabilityDigestCandidates([new(7, "digest")]));
        var repository = Substitute.For<IAdmissionScannerCapabilityRepository>();
        repository.FindByDigestCandidatesAsync(
                Arg.Any<IReadOnlyList<AdmissionScannerCapabilityDigestCandidate>>(),
                Arg.Any<CancellationToken>())
            .Returns(capability);
        var service = new AdmissionScannerAuthenticationService(
            material, repository, new FixedTimeProvider(UtcNow));

        AdmissionScannerAuthenticationResult result = await service.AuthenticateAsync(
            new AdmissionScannerAuthenticationRequest("stolen-or-invalid"), CancellationToken.None);

        await Assert.That(result).IsEqualTo(AdmissionScannerAuthenticationResult.Failed());
        await Assert.That(result.ToString()).IsEqualTo(
            "AdmissionScannerAuthenticationResult(authenticated=False, <redacted>)");
    }

    [Test]
    public async Task MalformedOrUnboundedCandidatesFailBeforeEntityLookup()
    {
        var material = Substitute.For<IAdmissionScannerCapabilityMaterialService>();
        material.DigestCandidatesAsync(
                Arg.Any<AdmissionScannerCapabilityDigestCandidatesRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new AdmissionScannerCapabilityDigestCandidates(
                Enumerable.Range(1, 9).Select(version =>
                    new AdmissionScannerCapabilityDigestCandidate(version, $"digest-{version}")).ToArray()));
        var repository = Substitute.For<IAdmissionScannerCapabilityRepository>();
        var service = new AdmissionScannerAuthenticationService(
            material, repository, new FixedTimeProvider(UtcNow));

        AdmissionScannerAuthenticationResult result = await service.AuthenticateAsync(
            new AdmissionScannerAuthenticationRequest("opaque"), CancellationToken.None);

        await Assert.That(result.Authenticated).IsFalse();
        await repository.DidNotReceiveWithAnyArgs().FindByDigestCandidatesAsync(default!, default);
    }

    [Test]
    public async Task DigestOptionsBoundCurrentAndRetainedVersions()
    {
        var valid = new AdmissionScannerCapabilityDigestOptions
        {
            ActiveKeyVersion = 8,
            RetainedKeyVersions = [7, 6, 5]
        };
        valid.Validate();

        var duplicate = new AdmissionScannerCapabilityDigestOptions
        {
            ActiveKeyVersion = 8,
            RetainedKeyVersions = [8]
        };
        var excessive = new AdmissionScannerCapabilityDigestOptions
        {
            ActiveKeyVersion = 9,
            RetainedKeyVersions = Enumerable.Range(1,
                AdmissionScannerCapabilityDigestOptions.MaximumKeyVersions).ToArray()
        };

        await Assert.That(duplicate.Validate).Throws<InvalidOperationException>();
        await Assert.That(excessive.Validate).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task DependencyFailureUsesTheSameGenericSecretFreeRejection()
    {
        var material = Substitute.For<IAdmissionScannerCapabilityMaterialService>();
        material.DigestCandidatesAsync(
                Arg.Any<AdmissionScannerCapabilityDigestCandidatesRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException<AdmissionScannerCapabilityDigestCandidates>(
                new InvalidOperationException("crypto detail")));
        var service = new AdmissionScannerAuthenticationService(
            material,
            Substitute.For<IAdmissionScannerCapabilityRepository>(),
            new FixedTimeProvider(UtcNow));

        const string plaintext = "scanner-bearer-secret";
        AdmissionScannerAuthenticationResult result = await service.AuthenticateAsync(
            new AdmissionScannerAuthenticationRequest(plaintext), CancellationToken.None);

        await Assert.That(result.Authenticated).IsFalse();
        await Assert.That(result.ToString()).DoesNotContain("crypto detail");
        await Assert.That(result.ToString()).DoesNotContain(plaintext);
    }

    [Test]
    public async Task CancellationPropagatesToDigestAndLookup()
    {
        using var cancellation = new CancellationTokenSource();
        var material = Substitute.For<IAdmissionScannerCapabilityMaterialService>();
        material.DigestCandidatesAsync(
                Arg.Any<AdmissionScannerCapabilityDigestCandidatesRequest>(), cancellation.Token)
            .Returns(new AdmissionScannerCapabilityDigestCandidates([new(7, "digest")]));
        var repository = Substitute.For<IAdmissionScannerCapabilityRepository>();
        repository.FindByDigestCandidatesAsync(
                Arg.Any<IReadOnlyList<AdmissionScannerCapabilityDigestCandidate>>(), cancellation.Token)
            .Returns(Task.FromException<AdmissionScannerCapability?>(
                new OperationCanceledException(cancellation.Token)));
        var service = new AdmissionScannerAuthenticationService(
            material, repository, new FixedTimeProvider(UtcNow));

        await Assert.ThrowsAsync<OperationCanceledException>(() => service.AuthenticateAsync(
            new AdmissionScannerAuthenticationRequest("opaque"), cancellation.Token));
        await material.Received(1).DigestCandidatesAsync(
            Arg.Any<AdmissionScannerCapabilityDigestCandidatesRequest>(), cancellation.Token);
        await repository.Received(1).FindByDigestCandidatesAsync(
            Arg.Any<IReadOnlyList<AdmissionScannerCapabilityDigestCandidate>>(), cancellation.Token);
    }

    private static AdmissionScannerCapability Capability(DateTimeOffset expiresAt) =>
        AdmissionScannerCapability.Issue(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            7,
            "lookup-digest",
            "Door scanner",
            AdmissionScannerCapabilityAction.CheckIn | AdmissionScannerCapabilityAction.Undo,
            expiresAt.UtcDateTime,
            Guid.CreateVersion7(),
            UtcNow.AddHours(-1).UtcDateTime);

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
