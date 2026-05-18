// ABOUTME: Unit tests for explicit authorization policy package sync command handling.
// ABOUTME: Verifies safe publish-result mapping without exposing provider-specific transport details.

using Explore.Application.Authorization;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Features.InstanceOnboarding.Handlers.Commands;
using Explore.Application.Features.InstanceOnboarding.Requests.Commands;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Event.Application.UnitTests.Features.InstanceOnboarding.Commands;

public sealed class SyncAuthorizationPolicyPackageCommandHandlerTests
{
    private readonly IPolicyPackageService _policyPackageService = Substitute.For<IPolicyPackageService>();
    private readonly SyncAuthorizationPolicyPackageCommandHandler _handler;

    public SyncAuthorizationPolicyPackageCommandHandlerTests()
    {
        _handler = new SyncAuthorizationPolicyPackageCommandHandler(
            _policyPackageService,
            Substitute.For<ILogger<SyncAuthorizationPolicyPackageCommandHandler>>());
    }

    [Test]
    public async Task Handle_WhenPublishSucceeds_ReturnsSuccess()
    {
        _policyPackageService.PublishAsync(Arg.Any<CancellationToken>())
            .Returns(new PolicyPackagePublishResult(
                Succeeded: true,
                PackageId: "islamuevent-authorization-policies",
                ContentHash: "abc123",
                Message: "Authorization policy package synced.",
                PublishedAt: DateTimeOffset.UtcNow,
                Warnings: []));

        var result = await _handler.Handle(new SyncAuthorizationPolicyPackageCommand(), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Message).Contains("synced");
    }

    [Test]
    public async Task Handle_WhenPublishFails_ReturnsSafeFailureWithWarnings()
    {
        _policyPackageService.PublishAsync(Arg.Any<CancellationToken>())
            .Returns(new PolicyPackagePublishResult(
                Succeeded: false,
                PackageId: "islamuevent-authorization-policies",
                ContentHash: "abc123",
                Message: "Authorization policy package sync failed.",
                PublishedAt: DateTimeOffset.UtcNow,
                Warnings: ["Configure Cerbos Admin API credentials before publishing."])
            {
                IssueCode = PolicyPackageIssueCode.AdminApiNotConfigured
            });

        var result = await _handler.Handle(new SyncAuthorizationPolicyPackageCommand(), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).Contains("failed");
        await Assert.That(result.FailureCode).IsEqualTo(nameof(PolicyPackageIssueCode.AdminApiNotConfigured));
        await Assert.That(result.Errors).Contains("Configure Cerbos Admin API credentials before publishing.");
    }

    [Test]
    public async Task Handle_WhenPublisherThrows_ReturnsGenericFailure()
    {
        _policyPackageService.PublishAsync(Arg.Any<CancellationToken>())
            .Returns<Task<PolicyPackagePublishResult>>(_ => throw new InvalidOperationException("secret should not leak"));

        var result = await _handler.Handle(new SyncAuthorizationPolicyPackageCommand(), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).IsEqualTo("Authorization policy package sync failed.");
        await Assert.That(string.Join(' ', result.Errors)).DoesNotContain("secret should not leak");
    }
}
