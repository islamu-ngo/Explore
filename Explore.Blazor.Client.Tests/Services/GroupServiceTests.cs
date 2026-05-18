// ABOUTME: Unit tests for GroupService membership HAL collection adaptation.
// ABOUTME: Ensures group member action affordances survive service compatibility mapping.

using System.Text.Json;
using Explore.Blazor.Client.Helpers;

namespace Explore.Blazor.Client.Tests.Services;

public sealed class GroupServiceTests
{
    private readonly IEventApiClient _apiClient;
    private readonly GroupService _service;

    public GroupServiceTests()
    {
        _apiClient = Substitute.For<IEventApiClient>();
        var logger = Substitute.For<ILogger<GroupService>>();
        _service = new GroupService(new HttpClient(), _apiClient, logger);
    }

    [Test]
    public async Task GetGroupMembersWithAffordancesAsync_PreservesCollectionCreateAndItemLinks()
    {
        var groupId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var response = CreateGroupMemberCollection(memberId, withCreateLink: true, withItemLinks: true);

        _apiClient.GetGroupMembersAsync(groupId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(response);

        var result = await _service.GetGroupMembersWithAffordancesAsync(groupId);

        await Assert.That(result.CanCreate).IsTrue();
        await Assert.That(result.Members.Count).IsEqualTo(1);
        var member = result.Members.Single();
        await Assert.That(member.Id).IsEqualTo(memberId);
        await Assert.That(member.HasHalLink("edit")).IsTrue();
        await Assert.That(member.HasHalLink("delete")).IsTrue();
    }

    [Test]
    public async Task GetGroupMembersAsync_ReturnsMembersForExistingCompatibilityCallers()
    {
        var groupId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        _apiClient.GetGroupMembersAsync(groupId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateGroupMemberCollection(memberId, withCreateLink: false, withItemLinks: false));

        var members = await _service.GetGroupMembersAsync(groupId);

        await Assert.That(members.Count).IsEqualTo(1);
        await Assert.That(members.Single().Id).IsEqualTo(memberId);
    }

    [Test]
    public async Task GetGroupMembersWithAffordancesAsync_ReturnsEmptyResult_WhenApiFails()
    {
        var groupId = Guid.NewGuid();
        _apiClient.GetGroupMembersAsync(groupId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ApiException("API Error", 500, null, null, null));

        var result = await _service.GetGroupMembersWithAffordancesAsync(groupId);

        await Assert.That(result.CanCreate).IsFalse();
        await Assert.That(result.Members).IsEmpty();
    }

    private static HalCollectionResourceOfGroupMemberDto CreateGroupMemberCollection(Guid memberId, bool withCreateLink, bool withItemLinks)
    {
        var collection = new HalCollectionResourceOfGroupMemberDto
        {
            _links = withCreateLink
                ? new Dictionary<string, HalLink>
                {
                    ["create"] = new() { Href = "/api/groupmember", Method = "POST" }
                }
                : new Dictionary<string, HalLink>(),
            _embedded = new HalCollectionEmbeddedOfGroupMemberDto
            {
                Items =
                [
                    new HalResourceOfGroupMemberDto
                    {
                        Id = memberId,
                        UserEmail = "member@example.com",
                        RoleId = RoleHelper.GroupAdmin
                    }
                ]
            }
        };

        if (withItemLinks)
        {
            using var links = JsonDocument.Parse("{\"edit\":{\"href\":\"/api/groupmember/1\",\"method\":\"PUT\"},\"delete\":{\"href\":\"/api/groupmember/1\",\"method\":\"DELETE\"}}");
            collection._embedded.Items.Single().AdditionalProperties["_links"] = links.RootElement.Clone();
        }

        return collection;
    }
}
