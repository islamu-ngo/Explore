// ABOUTME: Unit tests for custom property admin service projection HAL mapping.
// ABOUTME: Ensures projection action links survive generated client HAL resources into UI models.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Models;
using Explore.Blazor.Client.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Explore.Blazor.Client.Tests.Services;

public sealed class CustomPropertyAdminServiceTests
{
    [Test]
    public async Task GetEventProjectionStatusAsync_PreservesStatusHalLinks()
    {
        var apiClient = Substitute.For<IEventApiClient>();
        var tenantId = Guid.NewGuid();
        apiClient.GetCustomPropertyProjectionStatusAsync(tenantId, null, null, Arg.Any<CancellationToken>())
            .Returns(new HalCollectionResourceOfProjectionStatusDto
            {
                _embedded = new HalCollectionEmbeddedOfProjectionStatusDto
                {
                    Items =
                    [
                        new HalResourceOfProjectionStatusDto
                        {
                            ProjectionName = "event_custom_property_projection",
                            ProjectionVersion = 1,
                            TenantId = tenantId,
                            RowsProcessed = 7,
                            _links = new Dictionary<string, Anonymous40>
                            {
                                ["rebuild"] = new() { Href = "/rebuild" },
                                ["drain-dirty-scopes"] = new() { Href = "/drain" }
                            }
                        }
                    ]
                }
            });
        var service = new CustomPropertyAdminService(apiClient, NullLogger<CustomPropertyAdminService>.Instance);

        var result = await service.GetEventProjectionStatusAsync(tenantId);

        var status = result.Single();
        await Assert.That(status.RowsProcessed).IsEqualTo(7);
        await Assert.That(status.HasLink("rebuild")).IsTrue();
        await Assert.That(status.HasLink("drain-dirty-scopes")).IsTrue();
        await Assert.That(status.HasLink("missing")).IsFalse();
    }

    [Test]
    public async Task GetDirtyScopesAsync_PreservesItemHalLinksAndPagination()
    {
        var apiClient = Substitute.For<IEventApiClient>();
        var tenantId = Guid.NewGuid();
        apiClient.GetCustomPropertyProjectionDirtyScopesAsync(tenantId, "projection", 2, 10, null, null, Arg.Any<CancellationToken>())
            .Returns(new HalCollectionResourceOfProjectionDirtyScopeDto
            {
                PageNumber = 2,
                PageSize = 10,
                TotalCount = 1,
                _embedded = new HalCollectionEmbeddedOfProjectionDirtyScopeDto
                {
                    Items =
                    [
                        new HalResourceOfProjectionDirtyScopeDto
                        {
                            Id = 42,
                            TenantId = tenantId,
                            ProjectionName = "projection",
                            ScopeType = 1,
                            ScopeId = Guid.NewGuid(),
                            Reason = "retry",
                            _links = new Dictionary<string, Anonymous39>
                            {
                                ["drain"] = new() { Href = "/drain" }
                            }
                        }
                    ]
                }
            });
        var service = new CustomPropertyAdminService(apiClient, NullLogger<CustomPropertyAdminService>.Instance);

        var result = await service.GetDirtyScopesAsync(tenantId, "projection", 2, 10);

        await Assert.That(result.PageNumber).IsEqualTo(2);
        await Assert.That(result.TotalCount).IsEqualTo(1);
        var scope = result.Items.Single();
        await Assert.That(scope.Id).IsEqualTo(42);
        await Assert.That(scope.HasLink("drain")).IsTrue();
    }
}
