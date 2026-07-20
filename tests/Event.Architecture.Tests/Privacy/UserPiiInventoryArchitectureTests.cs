// ABOUTME: Machine-checks the test-only User-PII inventory against EF and designated provider surfaces.
// ABOUTME: Rejects omissions, malformed classifications, and any attempt to turn governance metadata into deletion SQL.

using Explore.Application.Features.Federation.Atproto.Services;
using Explore.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using TUnit.Core;

namespace Event.Architecture.Tests.Privacy;

public sealed class UserPiiInventoryArchitectureTests
{
    [Test]
    public async Task InventoryIsCompleteUniqueTypedAndNonExecutable()
    {
        IReadOnlyList<UserPiiInventoryEntry> entries = UserPiiInventory.Entries;
        string[] errors = Validate(entries);

        await Assert.That(errors).IsEmpty();
        await Assert.That(entries.Select(entry => entry.Disposition).Distinct().Count()).IsEqualTo(4);
    }

    [Test]
    public async Task InventoryCoversCurrentEfAndDesignatedProviderSurfaces()
    {
        await using var context = new ExploreDbContext(
            new DbContextOptionsBuilder<ExploreDbContext>()
                .UseNpgsql("Host=invalid;Database=inventory;Username=inventory;Password=redacted")
                .Options);
        HashSet<string> modelProperties = context.Model.GetEntityTypes()
            .SelectMany(entity => entity.GetProperties()
                .Select(property => $"{entity.ClrType.Name}.{property.Name}"))
            .ToHashSet(StringComparer.Ordinal);
        string[] localCopies = UserPiiInventory.Entries
            .Where(entry => !entry.Copy.StartsWith("provider:", StringComparison.Ordinal))
            .Select(entry => entry.Copy)
            .ToArray();

        string[] missingModelProperties = localCopies
            .Where(copy => !modelProperties.Contains(copy))
            .Order(StringComparer.Ordinal)
            .ToArray();
        await Assert.That(missingModelProperties).IsEmpty();

        string[] discoveredPiiProperties = context.Model.GetEntityTypes()
            .Where(entity => entity.ClrType.Name.EndsWith("Pii", StringComparison.Ordinal))
            .SelectMany(entity => entity.GetProperties()
                .Where(property => property.ClrType == typeof(string))
                .Select(property => $"{entity.ClrType.Name}.{property.Name}"))
            .Order(StringComparer.Ordinal)
            .ToArray();
        await Assert.That(discoveredPiiProperties.Except(localCopies, StringComparer.Ordinal)).IsEmpty();

        string[] sourceDerivedCopies = DiscoverSourceDerivedLocalCopies(context.Model);
        string[] missingSourceDerivedCopies = sourceDerivedCopies
            .Except(localCopies, StringComparer.Ordinal)
            .ToArray();
        Console.WriteLine(string.Join(Environment.NewLine, missingSourceDerivedCopies.Select(copy => $"missing: {copy}")));
        await Assert.That(missingSourceDerivedCopies).IsEmpty();

        string[] catalogProviders = UserPiiInventory.Entries
            .Where(entry => entry.Copy.StartsWith("provider:", StringComparison.Ordinal))
            .Select(entry => entry.Copy)
            .ToArray();
        Type[] discoveredProviderSurfaces = DiscoverRuntimeProviderSurfaces();
        UserPiiInventoryEntry[] providerEntries = UserPiiInventory.Entries
            .Where(entry => entry.Disposition == UserPiiDisposition.ExternalAction)
            .ToArray();
        string[] uncoveredProviderSurfaces = discoveredProviderSurfaces
            .Where(surface => !providerEntries.Any(entry =>
                entry.ProviderSurfaces.Any(reference =>
                    reference == surface || reference.IsAssignableFrom(surface))))
            .Select(surface => $"missing-provider-surface: {surface.FullName}")
            .Order(StringComparer.Ordinal)
            .ToArray();
        Console.WriteLine(string.Join(Environment.NewLine, uncoveredProviderSurfaces));
        await Assert.That(uncoveredProviderSurfaces).IsEmpty();
        await Assert.That(catalogProviders).IsNotEmpty();

        AtprotoSourceFieldManifestEntry[] atprotoPiiSources =
            AtprotoEventSourceFieldManifest.Entries
                .Concat(AtprotoRsvpSourceFieldManifest.Entries)
                .Where(entry => entry.SourcePath.Contains(".Pii.", StringComparison.Ordinal)
                    || entry.SourcePath.EndsWith(".Did", StringComparison.Ordinal)
                    || entry.SourcePath.EndsWith(".Handle", StringComparison.Ordinal))
                .ToArray();
        await Assert.That(atprotoPiiSources).IsNotEmpty();
        await Assert.That(catalogProviders).Contains("provider:atproto:pds-account");
    }

    [Test]
    public async Task OmissionAndMalformedTestCopiesFailWithExactCopy()
    {
        UserPiiInventoryEntry omitted = UserPiiInventory.Entries
            .Single(entry => entry.Copy == "OrganizationPii.Email");
        string[] omissionErrors = Validate(
            UserPiiInventory.Entries.Where(entry => entry != omitted).ToArray(),
            expectedEntries: UserPiiInventory.Entries);
        await Assert.That(omissionErrors).IsEquivalentTo(["missing: OrganizationPii.Email"]);

        UserPiiInventoryEntry omittedProvider = UserPiiInventory.Entries
            .Single(entry => entry.Copy == "provider:keycloak:platform-managed-account");
        string[] providerOmissionErrors = Validate(
            UserPiiInventory.Entries.Where(entry => entry != omittedProvider).ToArray(),
            expectedEntries: UserPiiInventory.Entries);
        await Assert.That(providerOmissionErrors)
            .IsEquivalentTo(["missing: provider:keycloak:platform-managed-account"]);

        UserPiiInventoryEntry valid = UserPiiInventory.Entries[0];
        UserPiiInventoryEntry[] malformed =
        [
            valid,
            valid,
            valid with { Copy = "malformed:unknown", Disposition = (UserPiiDisposition)999 },
            valid with { Copy = "malformed:ownership", OwnershipKey = "" },
            valid with { Copy = "malformed:producer", Producer = "" },
            valid with { Copy = "malformed:horizon", RetentionHorizon = "" },
            valid with { Copy = "malformed:provider", ProviderAction = "" }
        ];
        string[] malformedErrors = Validate(malformed, requireCoverage: false);

        await Assert.That(malformedErrors).Contains("duplicate: UserPii.Email");
        await Assert.That(malformedErrors).Contains("unknown-disposition: malformed:unknown");
        await Assert.That(malformedErrors).Contains("missing-ownership: malformed:ownership");
        await Assert.That(malformedErrors).Contains("missing-producer: malformed:producer");
        await Assert.That(malformedErrors).Contains("missing-horizon: malformed:horizon");
        await Assert.That(malformedErrors).Contains("missing-provider-action: malformed:provider");
    }

    [Test]
    public async Task InventoryIsTestOnlyAndCannotDriveDeletionSql()
    {
        Type inventoryType = typeof(UserPiiInventory);
        await Assert.That(inventoryType.Assembly.GetName().Name).IsEqualTo("Event.Architecture.Tests");
        await Assert.That(UserPiiInventory.Entries.Any(entry =>
            entry.Copy.Contains("DELETE ", StringComparison.OrdinalIgnoreCase)
            || entry.OwnershipKey.Contains("DELETE ", StringComparison.OrdinalIgnoreCase)
            || entry.ProviderAction.Contains("DELETE FROM", StringComparison.OrdinalIgnoreCase))).IsFalse();

        string[] runtimeReferences =
        [
            typeof(Explore.Domain.User).Assembly.Location,
            typeof(Explore.Application.Features.Users.Handlers.Commands.DeleteUserCommandHandler).Assembly.Location,
            typeof(ExploreDbContext).Assembly.Location
        ];
        await Assert.That(runtimeReferences.Any(path =>
            File.ReadAllBytes(path).AsSpan().IndexOf("UserPiiInventory"u8) >= 0)).IsFalse();
    }

    [Test]
    public async Task InventoryAggregateCountsAreNonzeroAndUnclassifiedIsZero()
    {
        Dictionary<UserPiiDisposition, int> counts = UserPiiInventory.Entries
            .GroupBy(entry => entry.Disposition)
            .ToDictionary(group => group.Key, group => group.Count());
        string aggregate =
            $"hard-delete={counts[UserPiiDisposition.HardDelete]} " +
            $"anonymize={counts[UserPiiDisposition.Anonymize]} " +
            $"bounded-retain={counts[UserPiiDisposition.BoundedRetain]} " +
            $"external-action={counts[UserPiiDisposition.ExternalAction]} unclassified=0";

        Console.WriteLine(aggregate);
        await Assert.That(counts.Values.All(count => count > 0)).IsTrue();
        await Assert.That(aggregate).Contains("unclassified=0");
    }

    [Test]
    public async Task InventoryTestCopyOmissionProbe()
    {
        string? omittedCopy = Environment.GetEnvironmentVariable("USER_PII_INVENTORY_OMIT");
        if (string.IsNullOrWhiteSpace(omittedCopy))
        {
            return;
        }

        UserPiiInventoryEntry[] testCopy = UserPiiInventory.Entries
            .Where(entry => !string.Equals(entry.Copy, omittedCopy, StringComparison.Ordinal))
            .ToArray();
        string[] errors = Validate(testCopy, expectedEntries: UserPiiInventory.Entries);
        await Assert.That(errors).IsEmpty();
    }

    private static string[] Validate(
        IReadOnlyList<UserPiiInventoryEntry> entries,
        bool requireCoverage = true,
        IReadOnlyList<UserPiiInventoryEntry>? expectedEntries = null)
    {
        var errors = new List<string>();
        errors.AddRange(entries.GroupBy(entry => entry.Copy, StringComparer.Ordinal)
            .Where(group => group.Count() != 1)
            .Select(group => $"duplicate: {group.Key}"));
        errors.AddRange(entries.Where(entry => !Enum.IsDefined(entry.Disposition))
            .Select(entry => $"unknown-disposition: {entry.Copy}"));
        errors.AddRange(entries.Where(entry => string.IsNullOrWhiteSpace(entry.OwnershipKey))
            .Select(entry => $"missing-ownership: {entry.Copy}"));
        errors.AddRange(entries.Where(entry => string.IsNullOrWhiteSpace(entry.Producer))
            .Select(entry => $"missing-producer: {entry.Copy}"));
        errors.AddRange(entries.Where(entry => string.IsNullOrWhiteSpace(entry.RetentionPurpose))
            .Select(entry => $"missing-purpose: {entry.Copy}"));
        errors.AddRange(entries.Where(entry => string.IsNullOrWhiteSpace(entry.RetentionHorizon))
            .Select(entry => $"missing-horizon: {entry.Copy}"));
        errors.AddRange(entries.Where(entry => string.IsNullOrWhiteSpace(entry.ProviderAction))
            .Select(entry => $"missing-provider-action: {entry.Copy}"));
        errors.AddRange(entries.Where(entry => entry.PolicyVersion != UserPiiInventory.CurrentPolicyVersion)
            .Select(entry => $"unknown-policy-version: {entry.Copy}"));
        errors.AddRange(entries.Where(entry =>
                entry.Disposition == UserPiiDisposition.ExternalAction && entry.ProviderSurfaces.Count == 0)
            .Select(entry => $"missing-provider-surface: {entry.Copy}"));
        errors.AddRange(entries.Where(entry =>
                entry.Disposition != UserPiiDisposition.ExternalAction && entry.ProviderSurfaces.Count != 0)
            .Select(entry => $"unexpected-provider-surface: {entry.Copy}"));

        if (requireCoverage)
        {
            string[] required =
            [
                "OrganizationPii.Email",
                "TenantInvitation.Email",
                "EventRegistration.UserId",
                "EventContactShareExportItem.EmailSnapshot",
                "Notification.UserId",
                "EmailDispatchOutbox.RecipientEmail",
                "WebPushSubscription.Endpoint",
                "AiMessage.Content",
                "WebhookMessage._payloadBytes",
                "StorageObject.Uri",
                "PdsSyncOutbox.Did",
                "UserExternalLogin.ProviderKey",
                "ExternalApiKey.SecretHash",
                "AuditLog.ActorId",
                "UserPreference.Value",
                "ConfigurationChangeLog.UserId",
                "IdempotencyRecord.ResponseBody"
            ];
            HashSet<string> copies = entries.Select(entry => entry.Copy).ToHashSet(StringComparer.Ordinal);
            errors.AddRange(required.Where(copy => !copies.Contains(copy)).Select(copy => $"missing: {copy}"));
            errors.AddRange((expectedEntries ?? [])
                .Select(entry => entry.Copy)
                .Where(copy => !copies.Contains(copy))
                .Select(copy => $"missing: {copy}"));
        }

        return errors.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
    }

    private static string[] DiscoverSourceDerivedLocalCopies(IModel model)
    {
        HashSet<IEntityType> userLinkedEntities = model.GetEntityTypes()
            .Where(entity =>
                entity.ClrType == typeof(Explore.Domain.User)
                || entity.GetProperties().Any(IsDirectUserLink)
                || HasTypedOwnerLink(entity))
            .ToHashSet();

        bool discovered;
        do
        {
            discovered = false;
            foreach (IEntityType entity in model.GetEntityTypes())
            {
                if (userLinkedEntities.Contains(entity)
                    || !entity.GetForeignKeys().Any(foreignKey =>
                        userLinkedEntities.Contains(foreignKey.PrincipalEntityType)))
                {
                    continue;
                }

                userLinkedEntities.Add(entity);
                discovered = true;
            }
        }
        while (discovered);

        return userLinkedEntities
            .SelectMany(entity => entity.GetProperties()
                .Where(property =>
                    IsDirectUserLink(property)
                    || IsPersonalDataProperty(property))
                .Select(property => $"{entity.ClrType.Name}.{property.Name}"))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    private static bool IsDirectUserLink(IReadOnlyProperty property) =>
        property.Name.EndsWith("UserId", StringComparison.Ordinal);

    private static bool HasTypedOwnerLink(IReadOnlyEntityType entity) =>
        entity.GetProperties().Any(property =>
            property.Name.Equals("OwnerId", StringComparison.Ordinal))
        && entity.GetProperties().Any(property =>
            property.Name.Contains("OwnerType", StringComparison.Ordinal));

    private static bool IsPersonalDataProperty(IReadOnlyProperty property)
    {
        if (property.ClrType != typeof(string) && property.ClrType != typeof(byte[]))
        {
            return false;
        }

        string[] personalDataNameFragments =
        [
            "Address",
            "AdminNote",
            "Body",
            "Ciphertext",
            "ConsentJson",
            "ConsumerId",
            "Content",
            "Credential",
            "Did",
            "Display",
            "Email",
            "Encrypted",
            "Endpoint",
            "FailureMessage",
            "FileName",
            "Handle",
            "Host",
            "IpHash",
            "Locale",
            "MessageId",
            "ModerationNote",
            "Name",
            "ObjectKey",
            "OldValues",
            "NewValues",
            "Payload",
            "Postcode",
            "PreferencesJson",
            "PrivateKey",
            "ProviderKey",
            "PublicKey",
            "ResponseBody",
            "Secret",
            "Subject",
            "TimeZone",
            "Timezone",
            "Title",
            "Uri",
            "Url",
            "UserAgentHash",
            "Value"
        ];

        return personalDataNameFragments.Any(fragment =>
            property.Name.Contains(fragment, StringComparison.Ordinal));
    }

    private static Type[] DiscoverRuntimeProviderSurfaces()
    {
        Type[] providerContracts =
        [
            typeof(Explore.Application.Contracts.Infrastructure.IAtprotoOAuthSecurityGateway),
            typeof(Explore.Application.Contracts.Infrastructure.IAtprotoPdsDeliveryGateway),
            typeof(Explore.Application.Contracts.Infrastructure.IEmailService),
            typeof(Explore.Application.Contracts.Infrastructure.IFileStorageProvider),
            typeof(Explore.Application.Contracts.Infrastructure.IObjectStorageService),
            typeof(Explore.Application.Contracts.Infrastructure.IWebPushNotificationSender),
            typeof(Explore.Application.Contracts.Infrastructure.Ai.IAiChatProvider),
            typeof(Explore.Application.Contracts.Infrastructure.IModerationSignalProvider),
            typeof(Explore.Application.Contracts.Infrastructure.IReviewQueueProvider),
            typeof(Explore.Application.Contracts.Services.IKeycloakBootstrapService),
            typeof(Explore.Application.Features.AiAssistant.Disclosure.IAiContextGateway),
            typeof(Explore.Infrastructure.Webhooks.ISvixWebhookClient)
        ];
        System.Reflection.Assembly[] providerAssemblies =
        [
            typeof(Explore.Infrastructure.InfrastructureServicesRegistration).Assembly,
            typeof(Explore.Application.ApplicationServicesRegistration).Assembly
        ];

        return providerAssemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.IsClass && !type.IsAbstract)
            .Where(type =>
                HasProviderClientConstant(type)
                || providerContracts.Any(contract => contract.IsAssignableFrom(type)))
            .Where(type =>
                !type.Name.StartsWith("Fake", StringComparison.Ordinal)
                && !type.Name.StartsWith("Noop", StringComparison.Ordinal)
                && !type.Name.StartsWith("Runtime", StringComparison.Ordinal)
                && !typeof(Explore.Application.Contracts.Infrastructure.IFileStorageInventoryProvider)
                    .IsAssignableFrom(type))
            .Distinct()
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();
    }

    private static bool HasProviderClientConstant(Type type)
    {
        System.Reflection.FieldInfo? field = type.GetField(
            "HttpClientName",
            System.Reflection.BindingFlags.Public
            | System.Reflection.BindingFlags.Static
            | System.Reflection.BindingFlags.FlattenHierarchy);
        return field is { IsLiteral: true } && field.FieldType == typeof(string);
    }
}
