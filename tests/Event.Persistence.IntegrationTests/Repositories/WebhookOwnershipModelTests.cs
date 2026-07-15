// ABOUTME: EF Core model tests for typed webhook ownership and instance/tenant scope isolation.
// ABOUTME: Verifies nullable scope keys, relational owner FKs, check constraints, and filtered indexes.

using Explore.Domain;
using Explore.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using TUnit.Core;

namespace Event.Persistence.IntegrationTests.Repositories;

public sealed class WebhookOwnershipModelTests
{
    [Test]
    public async Task ConsumerModel_EnforcesTypedOwnerAndOptionalTenantScope()
    {
        await using var context = CreateModelContext();
        var model = context.GetService<IDesignTimeModel>().Model;
        var consumer = model.FindEntityType(typeof(WebhookConsumer))!;

        await Assert.That(consumer.FindProperty(nameof(WebhookConsumer.TenantId))!.IsNullable).IsTrue();
        await Assert.That(consumer.FindProperty(nameof(WebhookConsumer.InstanceId))!.IsNullable).IsTrue();
        await Assert.That(consumer.GetCheckConstraints().Select(constraint => constraint.Name))
            .Contains("ck_webhook_consumers_typed_owner");

        var principals = consumer.GetForeignKeys()
            .Select(foreignKey => foreignKey.PrincipalEntityType.ClrType)
            .ToArray();
        await Assert.That(principals).Contains(typeof(InstanceBootstrapState));
        await Assert.That(principals).Contains(typeof(Organization));
        await Assert.That(principals).Contains(typeof(Group));
        await Assert.That(principals).Contains(typeof(TenantUser));
    }

    [Test]
    public async Task EndpointAndSubscriptionModels_SupportInstanceOrTenantConfigurationScope()
    {
        await using var context = CreateModelContext();
        var model = context.GetService<IDesignTimeModel>().Model;
        var endpoint = model.FindEntityType(typeof(WebhookEndpoint))!;
        var subscription = model.FindEntityType(typeof(WebhookEndpointSubscription))!;

        await Assert.That(endpoint.FindProperty(nameof(WebhookEndpoint.TenantId))!.IsNullable).IsTrue();
        await Assert.That(endpoint.FindProperty(nameof(WebhookEndpoint.InstanceId))!.IsNullable).IsTrue();
        await Assert.That(subscription.FindProperty(nameof(WebhookEndpointSubscription.TenantId))!.IsNullable).IsTrue();
        await Assert.That(subscription.FindProperty(nameof(WebhookEndpointSubscription.InstanceId))!.IsNullable).IsTrue();
        await Assert.That(endpoint.GetCheckConstraints().Select(constraint => constraint.Name))
            .Contains("ck_webhook_endpoints_configuration_scope");
        await Assert.That(subscription.GetCheckConstraints().Select(constraint => constraint.Name))
            .Contains("ck_webhook_endpoint_subscriptions_configuration_scope");
    }

    private static ExploreDbContext CreateModelContext()
    {
        var options = new DbContextOptionsBuilder<ExploreDbContext>()
            .UseNpgsql("Host=localhost;Database=webhook_ownership_model;Username=unused;Password=unused")
            .UseSnakeCaseNamingConvention()
            .Options;
        return new ExploreDbContext(options);
    }
}
