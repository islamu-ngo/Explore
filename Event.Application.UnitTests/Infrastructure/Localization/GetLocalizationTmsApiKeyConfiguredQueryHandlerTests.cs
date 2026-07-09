// ABOUTME: Unit tests for localization TMS API-key binding presence checks.
// ABOUTME: Verifies tenant binding precedence and instance fallback without resolving secret values.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.Localization.Handlers.Queries;
using Explore.Application.Features.Localization.Requests.Queries;
using Explore.Domain.Enums;
using Explore.Domain.Secrets;
using NSubstitute;

namespace Event.Application.UnitTests.Infrastructure.Localization;

public class GetLocalizationTmsApiKeyConfiguredQueryHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    [Test]
    public async Task Handle_WhenTenantBindingExists_ReturnsTrueWithoutCheckingInstanceScope()
    {
        var repository = Substitute.For<ISecretBindingRepository>();
        repository.ExistsForScopeAsync(
                SecretDefinitionRegistry.Keys.Localization.TmsApiKey,
                SecretScope.Tenant,
                TenantId,
                Arg.Any<CancellationToken>())
            .Returns(true);
        var handler = CreateHandler(repository);

        var result = await handler.Handle(new GetLocalizationTmsApiKeyConfiguredQuery(), CancellationToken.None);

        await Assert.That(result).IsTrue();
        await repository.DidNotReceive().ExistsForScopeAsync(
            SecretDefinitionRegistry.Keys.Localization.TmsApiKey,
            SecretScope.Instance,
            null,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenOnlyInstanceBindingExists_ReturnsTrue()
    {
        var repository = Substitute.For<ISecretBindingRepository>();
        repository.ExistsForScopeAsync(
                SecretDefinitionRegistry.Keys.Localization.TmsApiKey,
                SecretScope.Instance,
                null,
                Arg.Any<CancellationToken>())
            .Returns(true);
        var handler = CreateHandler(repository);

        var result = await handler.Handle(new GetLocalizationTmsApiKeyConfiguredQuery(), CancellationToken.None);

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task Handle_WhenNoBindingExists_ReturnsFalse()
    {
        var handler = CreateHandler(Substitute.For<ISecretBindingRepository>());

        var result = await handler.Handle(new GetLocalizationTmsApiKeyConfiguredQuery(), CancellationToken.None);

        await Assert.That(result).IsFalse();
    }

    private static GetLocalizationTmsApiKeyConfiguredQueryHandler CreateHandler(
        ISecretBindingRepository repository)
    {
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.TenantId.Returns(TenantId);

        return new GetLocalizationTmsApiKeyConfiguredQueryHandler(tenantContext, repository);
    }
}
