// ABOUTME: Red contract tests for tenant directory-operator identity administration through the generated client.
// ABOUTME: Requires exact HAL edit authority, grouped PATCH values, revision chaining, and authoritative conflict reload.

using System.Reflection;

namespace Explore.Blazor.Client.Tests.Services;

public sealed class TenantDirectoryOperatorIdentityAdminServiceTests
{
    private const string ServiceTypeName =
        "Explore.Blazor.Client.Services.TenantDirectoryOperatorIdentityAdminService";

    private readonly IEventApiClient _api = Substitute.For<IEventApiClient>();

    [Test]
    public async Task GetAsync_MapsAllIdentityAndReadinessGroups_FromExactEditAffordanceOnly()
    {
        Type? serviceType = ResolveProductionType(ServiceTypeName);
        await Assert.That(serviceType).IsNotNull();
        if (serviceType is null) return;

        Guid revision = Guid.Parse("11111111-1111-1111-1111-111111111111");
        _api.GetTenantDirectoryOperatorIdentityDocumentAsync(
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateDocument(revision, includeExactEdit: true));

        object model = await InvokeAsync(CreateService(serviceType), "GetAsync", CancellationToken.None);

        await Assert.That(Read<bool>(model, "CanEdit")).IsTrue();
        await Assert.That(Read<Guid>(model, "ConcurrencyStamp")).IsEqualTo(revision);
        await Assert.That(Read<string>(model, "PublicName")).IsEqualTo("Community Directory");
        await Assert.That(Read<string>(model, "LegalName")).IsEqualTo("Community Directory Foundation");
        await Assert.That(Read<string>(model, "OperatorKindCode")).IsEqualTo("NONPROFIT");
        await Assert.That(Read<string>(model, "JurisdictionCountryCode")).IsEqualTo("DE");
        await Assert.That(Read<string>(model, "RegistrationIdentifier")).IsEqualTo("VR 12345");
        await Assert.That(Read<string>(model, "PublicContactEmail")).IsEqualTo("support@directory.example");
        await Assert.That(Read<string>(model, "LegalNoticeUrl")).IsEqualTo("https://directory.example/legal");
        await Assert.That(Read<string>(model, "TermsUrl")).IsEqualTo("https://directory.example/terms");
        await Assert.That(Read<string>(model, "PrivacyUrl")).IsEqualTo("https://directory.example/privacy");
        await Assert.That(Read<bool>(model, "IsActivationReady")).IsTrue();
        await Assert.That(Read<bool>(model, "IsPublicDisclosureReady")).IsTrue();
        await Assert.That(Read<bool>(model, "IsPaidCommerceReady")).IsTrue();
    }

    [Test]
    public async Task GetAsync_DoesNotInferEditAuthority_FromDtoFlagOrNearMatchLinks()
    {
        Type? serviceType = ResolveProductionType(ServiceTypeName);
        await Assert.That(serviceType).IsNotNull();
        if (serviceType is null) return;

        foreach (HalResourceOfTenantDirectoryOperatorIdentityDocumentDto document in new[]
                 {
                     CreateDocument(Guid.NewGuid(), canEditFlag: true),
                     CreateDocument(Guid.NewGuid(), linkRel: "Edit", linkMethod: "PATCH"),
                     CreateDocument(Guid.NewGuid(), linkRel: "edit", linkMethod: "GET")
                 })
        {
            _api.ClearReceivedCalls();
            _api.GetTenantDirectoryOperatorIdentityDocumentAsync(
                    Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
                .Returns(document);

            object model = await InvokeAsync(CreateService(serviceType), "GetAsync", CancellationToken.None);

            await Assert.That(Read<bool>(model, "CanEdit")).IsFalse();
        }
    }

    [Test]
    public async Task GetAsync_RejectsExternalWrongAndQueryEditHrefs()
    {
        Type serviceType = ResolveProductionType(ServiceTypeName)!;
        foreach (string href in new[]
                 {
                     "https://evil.example/api/tenant/settings/documents/directory-operator-identity",
                     "/api/tenant/settings/documents/other",
                     "/api/tenant/settings/documents/directory-operator-identity?override=true"
                 })
        {
            HalResourceOfTenantDirectoryOperatorIdentityDocumentDto document =
                CreateDocument(Guid.NewGuid(), includeExactEdit: true);
            document._links!["edit"].Href = href;
            _api.GetTenantDirectoryOperatorIdentityDocumentAsync(
                    Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
                .Returns(document);

            object model = await InvokeAsync(CreateService(serviceType), "GetAsync", CancellationToken.None);

            await Assert.That(Read<bool>(model, "CanEdit")).IsFalse();
        }
    }

    [Test]
    public async Task SaveAsync_SendsGroupedPatchWithCurrentRevision_AndChainsReturnedRevision()
    {
        Type? serviceType = ResolveProductionType(ServiceTypeName);
        await Assert.That(serviceType).IsNotNull();
        if (serviceType is null) return;

        Guid currentRevision = Guid.Parse("22222222-2222-2222-2222-222222222222");
        Guid updatedRevision = Guid.Parse("33333333-3333-3333-3333-333333333333");
        PatchTenantDirectoryOperatorIdentityDocumentDto? observed = null;
        _api.GetTenantDirectoryOperatorIdentityDocumentAsync(
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateDocument(currentRevision, includeExactEdit: true));
        _api.PatchTenantDirectoryOperatorIdentityDocumentAsync(
                Arg.Do<PatchTenantDirectoryOperatorIdentityDocumentDto>(request => observed = request),
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateDocument(updatedRevision, includeExactEdit: true, publicName: "Updated Directory"));
        object service = CreateService(serviceType);
        object model = await InvokeAsync(service, "GetAsync", CancellationToken.None);

        object result = await InvokeAsync(service, "SaveAsync", model, CancellationToken.None);

        await Assert.That(observed).IsNotNull();
        await Assert.That(observed!.ExpectedConcurrencyStamp).IsEqualTo(currentRevision);
        await Assert.That(observed.LegalEntity).IsNotNull();
        await Assert.That(observed.Contacts).IsNotNull();
        await Assert.That(observed.LegalLinks).IsNotNull();
        await Assert.That(observed.LegalEntity!.PublicName?.Value).IsEqualTo("Community Directory");
        await Assert.That(observed.LegalEntity.LegalName?.Value).IsEqualTo("Community Directory Foundation");
        await Assert.That(observed.LegalEntity.OperatorKindCode?.Value).IsEqualTo("NONPROFIT");
        await Assert.That(observed.LegalEntity.JurisdictionCountryCode?.Value).IsEqualTo("DE");
        await Assert.That(observed.LegalEntity.RegistrationIdentifier?.Value).IsEqualTo("VR 12345");
        await Assert.That(observed.Contacts!.PublicContactEmail?.Value).IsEqualTo("support@directory.example");
        await Assert.That(observed.LegalLinks!.LegalNoticeUrl?.Value).IsEqualTo("https://directory.example/legal");
        await Assert.That(observed.LegalLinks.TermsUrl?.Value).IsEqualTo("https://directory.example/terms");
        await Assert.That(observed.LegalLinks.PrivacyUrl?.Value).IsEqualTo("https://directory.example/privacy");
        await Assert.That(Read<bool>(result, "Success")).IsTrue();
        object updatedModel = Read<object>(result, "Model");
        await Assert.That(Read<Guid>(updatedModel, "ConcurrencyStamp")).IsEqualTo(updatedRevision);
        await Assert.That(Read<string>(updatedModel, "PublicName")).IsEqualTo("Updated Directory");
    }

    [Test]
    public async Task SaveAsync_WhenPatchConflicts_ReloadsAndReturnsAuthoritativeState()
    {
        Type? serviceType = ResolveProductionType(ServiceTypeName);
        await Assert.That(serviceType).IsNotNull();
        if (serviceType is null) return;

        Guid initialRevision = Guid.Parse("44444444-4444-4444-4444-444444444444");
        Guid authoritativeRevision = Guid.Parse("55555555-5555-5555-5555-555555555555");
        _api.GetTenantDirectoryOperatorIdentityDocumentAsync(
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(
                CreateDocument(initialRevision, includeExactEdit: true),
                CreateDocument(authoritativeRevision, includeExactEdit: true, publicName: "Authoritative Directory"));
        _api.PatchTenantDirectoryOperatorIdentityDocumentAsync(
                Arg.Any<PatchTenantDirectoryOperatorIdentityDocumentDto>(),
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns<Task<HalResourceOfTenantDirectoryOperatorIdentityDocumentDto>>(_ => throw new ApiException(
                "Conflict", 409, string.Empty, new Dictionary<string, IEnumerable<string>>(), null));
        object service = CreateService(serviceType);
        object model = await InvokeAsync(service, "GetAsync", CancellationToken.None);

        object result = await InvokeAsync(service, "SaveAsync", model, CancellationToken.None);

        await Assert.That(Read<bool>(result, "Success")).IsFalse();
        await Assert.That(Read<bool>(result, "IsConcurrencyConflict")).IsTrue();
        object authoritative = Read<object>(result, "Model");
        await Assert.That(Read<Guid>(authoritative, "ConcurrencyStamp")).IsEqualTo(authoritativeRevision);
        await Assert.That(Read<string>(authoritative, "PublicName")).IsEqualTo("Authoritative Directory");
        await _api.Received(2).GetTenantDirectoryOperatorIdentityDocumentAsync(
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
        await _api.Received(1).PatchTenantDirectoryOperatorIdentityDocumentAsync(
            Arg.Any<PatchTenantDirectoryOperatorIdentityDocumentDto>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    private object CreateService(Type serviceType)
    {
        Type loggerType = typeof(ILogger<>).MakeGenericType(serviceType);
        object logger = Substitute.For([loggerType], []);
        return Activator.CreateInstance(serviceType, _api, logger)
            ?? throw new InvalidOperationException($"Could not create {serviceType.Name}.");
    }

    private static Type? ResolveProductionType(string fullName) =>
        typeof(TenantBrandingSettingsAdminService).Assembly.GetType(fullName, throwOnError: false);

    private static async Task<object> InvokeAsync(object target, string methodName, params object[] arguments)
    {
        MethodInfo method = target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Single(candidate => candidate.Name == methodName && candidate.GetParameters().Length == arguments.Length);
        var task = (Task)(method.Invoke(target, arguments)
            ?? throw new InvalidOperationException($"{target.GetType().Name}.{methodName} returned null."));
        await task;
        return task.GetType().GetProperty("Result")?.GetValue(task)
            ?? throw new InvalidOperationException($"{target.GetType().Name}.{methodName} did not return a result.");
    }

    private static T Read<T>(object target, string propertyName) =>
        (T)(target.GetType().GetProperty(propertyName)?.GetValue(target)
            ?? throw new InvalidOperationException($"{target.GetType().Name} does not expose {propertyName}."));

    private static HalResourceOfTenantDirectoryOperatorIdentityDocumentDto CreateDocument(
        Guid revision,
        bool includeExactEdit = false,
        bool canEditFlag = false,
        string? linkRel = null,
        string linkMethod = "PATCH",
        string publicName = "Community Directory")
    {
        var document = new HalResourceOfTenantDirectoryOperatorIdentityDocumentDto
        {
            ConcurrencyStamp = revision,
            CanEdit = canEditFlag,
            IsActivationReady = true,
            IsPublicDisclosureReady = true,
            IsPaidCommerceReady = true,
            Payload = new Payload2
            {
                PublicName = publicName,
                LegalName = "Community Directory Foundation",
                OperatorKindCode = "NONPROFIT",
                JurisdictionCountryCode = "DE",
                RegistrationIdentifier = "VR 12345",
                PublicContactEmail = "support@directory.example",
                LegalNoticeUrl = "https://directory.example/legal",
                TermsUrl = "https://directory.example/terms",
                PrivacyUrl = "https://directory.example/privacy"
            }
        };

        string? rel = includeExactEdit ? "edit" : linkRel;
        if (rel is not null)
        {
            document._links = new Dictionary<string, HalLink>
            {
                [rel] = new()
                {
                    Href = "/api/tenant/settings/documents/directory-operator-identity",
                    Method = linkMethod
                }
            };
        }

        return document;
    }
}
