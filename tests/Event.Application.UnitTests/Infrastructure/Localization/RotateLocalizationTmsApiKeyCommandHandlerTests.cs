// ABOUTME: Unit tests for rotating localization TMS API-key SecretBinding metadata.
// ABOUTME: Verifies backend-only inline encryption, cache invalidation, and instance lock enforcement.

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Secrets;
using Explore.Application.DTOs.Localization;
using Explore.Application.Features.Localization.Handlers.Commands;
using Explore.Application.Features.Localization.Requests.Commands;
using Explore.Domain.Enums;
using Explore.Domain.Secrets;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Event.Application.UnitTests.Infrastructure.Localization;

public class RotateLocalizationTmsApiKeyCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid ActorId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private readonly IAdminContext _adminContext;
    private readonly ISecretBindingRepository _repository;
    private readonly ISecretResolver _secretResolver;
    private readonly RotateLocalizationTmsApiKeyCommandHandler _handler;

    public RotateLocalizationTmsApiKeyCommandHandlerTests()
    {
        _adminContext = Substitute.For<IAdminContext>();
        _adminContext.ResolveUserIdAsync(Arg.Any<CancellationToken>()).Returns(ActorId);
        _adminContext.IsInstanceAdminAsync(ActorId, Arg.Any<CancellationToken>()).Returns(true);

        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.TenantId.Returns(TenantId);

        _repository = Substitute.For<ISecretBindingRepository>();
        var protector = Substitute.For<IInlineSecretProtector>();
        protector.Protect(Arg.Any<string>()).Returns(new InlineProtectedSecret(new byte[] { 1, 2, 3 }, 1));
        _secretResolver = Substitute.For<ISecretResolver>();

        _handler = new RotateLocalizationTmsApiKeyCommandHandler(
            _adminContext,
            tenantContext,
            _repository,
            protector,
            _secretResolver,
            Substitute.For<ILogger<RotateLocalizationTmsApiKeyCommandHandler>>());
    }

    [Test]
    public async Task Handle_WhenTenantAdminRotatesKey_CreatesInlineTenantBindingAndInvalidatesCache()
    {
        var command = new RotateLocalizationTmsApiKeyCommand
        {
            Dto = new RotateLocalizationTmsApiKeyDto { TmsApiKey = " test-key " }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await _repository.Received(1).Create(Arg.Is<SecretBinding>(binding =>
            binding.SettingKey == SecretDefinitionRegistry.Keys.Localization.TmsApiKey
            && binding.Scope == SecretScope.Tenant
            && binding.ScopeId == TenantId
            && binding.SourceType == SecretSourceType.InlineEncrypted
            && binding.InlineCiphertext!.SequenceEqual(new byte[] { 1, 2, 3 })
            && binding.InlineCiphertextVersion == 1
            && binding.CreatedBy == ActorId
            && binding.UpdatedBy == ActorId));
        await _secretResolver.Received(1).InvalidateAsync(
            SecretDefinitionRegistry.Keys.Localization.TmsApiKey,
            SecretScope.Tenant,
            TenantId,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenInstanceBindingIsLocked_ReturnsFailureWithoutPersisting()
    {
        var lockedBinding = SecretBinding.CreateEnvironmentVariable(
            SecretDefinitionRegistry.Keys.Localization.TmsApiKey,
            SecretScope.Instance,
            null,
            "LOCALIZATION_TMS_API_KEY",
            isLocked: true);
        _repository.GetByKeyAndScopeAsync(
                SecretDefinitionRegistry.Keys.Localization.TmsApiKey,
                SecretScope.Instance,
                null,
                Arg.Any<CancellationToken>())
            .Returns(lockedBinding);
        var command = new RotateLocalizationTmsApiKeyCommand
        {
            Dto = new RotateLocalizationTmsApiKeyDto { TmsApiKey = "test-key" }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await _repository.DidNotReceive().Create(Arg.Any<SecretBinding>());
        await _repository.DidNotReceive().Update(Arg.Any<SecretBinding>());
    }
}
