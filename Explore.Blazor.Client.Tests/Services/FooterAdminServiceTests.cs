// ABOUTME: Unit tests for FooterAdminService generated-client delegation and fallback behavior.
// ABOUTME: Verifies footer operation calls, generated DTO forwarding, and resilient command failures.

namespace Explore.Blazor.Client.Tests.Services;

public sealed class FooterAdminServiceTests
{
    private readonly IEventApiClient _apiClient = Substitute.For<IEventApiClient>();
    private readonly ILogger<FooterAdminService> _logger = Substitute.For<ILogger<FooterAdminService>>();

    [Test]
    public async Task GetLinkGroupsAsync_ReturnsGroups_WhenApiSucceeds()
    {
        ICollection<FooterLinkGroupListDto> groups =
        [
            new() { Id = Guid.NewGuid(), Title = "Main", IsActive = true }
        ];
        _apiClient.GetFooterLinkGroupsAsync(
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(groups);
        var service = CreateService();

        var result = await service.GetLinkGroupsAsync();

        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result[0].Title).IsEqualTo("Main");
        await _apiClient.Received(1).GetFooterLinkGroupsAsync(
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetFooterSettingsAsync_ReturnsSettings_FromConfigEnvelope()
    {
        var settings = new FooterSettingsDto
        {
            Enabled = true,
            Template = "compact"
        };
        _apiClient.GetFooterConfigAsync(
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(new FooterConfigDto { Settings = settings });
        var service = CreateService();

        var result = await service.GetFooterSettingsAsync();

        await Assert.That(result).IsSameReferenceAs(settings);
        await Assert.That(result!.Enabled).IsTrue();
        await Assert.That(result.Template).IsEqualTo("compact");
    }

    [Test]
    public async Task CreateLinkGroupAsync_ReturnsFailureResponse_WhenApiRejectsRequest()
    {
        _apiClient.CreateFooterLinkGroupAsync(
                Arg.Any<CreateFooterLinkGroupRequest>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns<Task<BaseCommandResponseOfGuid>>(_ => throw new ApiException(
                "Bad Request",
                400,
                string.Empty,
                new Dictionary<string, IEnumerable<string>>(),
                null));
        var service = CreateService();
        var request = new CreateFooterLinkGroupRequest { Title = "Main" };

        var result = await service.CreateLinkGroupAsync(request);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).IsEqualTo("API error (400).");
        await Assert.That(result.Errors).Contains("API error (400).");
        await _apiClient.Received(1).CreateFooterLinkGroupAsync(
            request,
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UpdateLinkAsync_ForwardsIdAndRequestAndReturnsSuccess()
    {
        var expected = new BaseCommandResponseOfGuid
        {
            Success = true,
            Message = "updated",
            Id = Guid.NewGuid()
        };
        _apiClient.UpdateFooterLinkAsync(
                Arg.Any<Guid>(),
                Arg.Any<UpdateFooterLinkRequest>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(expected);
        var service = CreateService();
        var linkId = Guid.NewGuid();
        var request = new UpdateFooterLinkRequest { Label = "Docs", Url = "/docs", IsActive = true };

        var result = await service.UpdateLinkAsync(linkId, request);

        await Assert.That(result).IsSameReferenceAs(expected);
        await Assert.That(result.Success).IsTrue();
        await _apiClient.Received(1).UpdateFooterLinkAsync(
            linkId,
            request,
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeleteLinkAsync_ReturnsFalse_WhenApiThrows()
    {
        _apiClient.DeleteFooterLinkAsync(
                Arg.Any<Guid>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns<Task<bool>>(_ => throw new HttpRequestException("network failed"));
        var service = CreateService();

        var result = await service.DeleteLinkAsync(Guid.NewGuid());

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task UpdateTenantSettingsAsync_ForwardsRequestAndReturnsSuccess()
    {
        var expected = new BaseCommandResponseOfGuid
        {
            Success = true,
            Message = "updated",
            Id = Guid.NewGuid()
        };
        _apiClient.UpdateTenantFooterSettingsAsync(
                Arg.Any<UpdateTenantFooterSettingsRequest>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(expected);
        var service = CreateService();
        var request = new UpdateTenantFooterSettingsRequest { Enabled = true };

        var result = await service.UpdateTenantSettingsAsync(request);

        await Assert.That(result).IsSameReferenceAs(expected);
        await Assert.That(result.Success).IsTrue();
        await _apiClient.Received(1).UpdateTenantFooterSettingsAsync(
            request,
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    private FooterAdminService CreateService() => new(_apiClient, _logger);
}
