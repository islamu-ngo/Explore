// ABOUTME: Unit tests for tenant branding settings mapping through the generated Event API client.
// ABOUTME: Verifies HAL-gated replacement, generated request normalization, and safe load failures.

namespace Explore.Blazor.Client.Tests.Services;

public sealed class TenantBrandingSettingsAdminServiceTests
{
    private readonly IEventApiClient _api = Substitute.For<IEventApiClient>();
    private readonly TenantBrandingSettingsAdminService _service;

    public TenantBrandingSettingsAdminServiceTests()
    {
        _service = new TenantBrandingSettingsAdminService(
            _api,
            Substitute.For<ILogger<TenantBrandingSettingsAdminService>>());
    }

    [Test]
    public async Task GetAsync_WhenApiReturnsHalDocument_MapsPayloadAndReplaceAffordance()
    {
        var stamp = Guid.Parse("11111111-1111-1111-1111-111111111111");
        _api.GetTenantBrandingSettingsDocumentAsync(
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(CreateDocument(stamp, "Typed Tenant", "https://cdn.example.test/tenant.css"));

        var result = await _service.GetAsync();

        await Assert.That(result.Exists).IsTrue();
        await Assert.That(result.CanReplace).IsTrue();
        await Assert.That(result.ConcurrencyStamp).IsEqualTo(stamp);
        await Assert.That(result.DisplayName).IsEqualTo("Typed Tenant");
        await Assert.That(result.CustomCssUrl).IsEqualTo("https://cdn.example.test/tenant.css");
    }

    [Test]
    public async Task GetAsync_WhenApiReturnsNotFound_ReportsMissingDocument()
    {
        _api.GetTenantBrandingSettingsDocumentAsync(
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns<Task<HalResourceOfTenantBrandingSettingsDocumentDto>>(_ => throw new ApiException(
                "Not found",
                404,
                string.Empty,
                new Dictionary<string, IEnumerable<string>>(),
                null));

        var result = await _service.GetAsync();

        await Assert.That(result.Exists).IsFalse();
        await Assert.That(result.CanReplace).IsFalse();
        await Assert.That(result.ErrorMessage)
            .IsEqualTo("Tenant branding settings have not been initialized.");
    }

    [Test]
    public async Task SaveAsync_WhenHalActionIsMissing_DoesNotCallApi()
    {
        var model = new TenantBrandingSettingsAdminModel
        {
            Exists = true,
            CanReplace = false,
            ConcurrencyStamp = Guid.NewGuid(),
            DisplayName = "No Access"
        };

        var result = await _service.SaveAsync(model);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message)
            .IsEqualTo("You do not have permission to replace tenant branding settings.");
        await _api.DidNotReceive().ReplaceTenantBrandingSettingsDocumentAsync(
            Arg.Any<ReplaceTenantBrandingSettingsDocumentDto>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SaveAsync_WhenApiSucceeds_ForwardsNormalizedRequestAndReturnsUpdatedModel()
    {
        var expectedStamp = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var updatedStamp = Guid.Parse("33333333-3333-3333-3333-333333333333");
        _api.ReplaceTenantBrandingSettingsDocumentAsync(
                Arg.Any<ReplaceTenantBrandingSettingsDocumentDto>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(CreateDocument(updatedStamp, "Updated Tenant", "https://cdn.example.test/updated.css"));
        var model = new TenantBrandingSettingsAdminModel
        {
            Exists = true,
            CanReplace = true,
            ConcurrencyStamp = expectedStamp,
            DisplayName = " Updated Tenant ",
            CustomCssUrl = " https://cdn.example.test/tenant.css "
        };

        var result = await _service.SaveAsync(model);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Model).IsNotNull();
        await Assert.That(result.Model!.ConcurrencyStamp).IsEqualTo(updatedStamp);
        await Assert.That(result.Model.DisplayName).IsEqualTo("Updated Tenant");
        await _api.Received(1).ReplaceTenantBrandingSettingsDocumentAsync(
            Arg.Is<ReplaceTenantBrandingSettingsDocumentDto>(request =>
                request.ExpectedConcurrencyStamp == expectedStamp &&
                request.Payload!.DisplayName == "Updated Tenant" &&
                request.Payload.CustomCssUrl == "https://cdn.example.test/tenant.css"),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    private static HalResourceOfTenantBrandingSettingsDocumentDto CreateDocument(
        Guid stamp,
        string displayName,
        string customCssUrl) => new()
        {
            ConcurrencyStamp = stamp,
            Payload = new Payload
            {
                DisplayName = displayName,
                CustomCssUrl = customCssUrl
            },
            _links = new Dictionary<string, HalLink>
            {
                ["self/replace-settings"] = new()
                {
                    Href = "/api/tenant/settings/documents/branding",
                    Method = "PUT"
                }
            }
        };
}
