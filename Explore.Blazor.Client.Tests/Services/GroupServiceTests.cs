// ABOUTME: Unit tests for GroupService membership HAL collection adaptation.
// ABOUTME: Ensures group member action affordances survive service compatibility mapping.

using System.Net;
using System.Text;
using System.Text.Json;
using Explore.Blazor.Client.Helpers;
using Refit;

namespace Explore.Blazor.Client.Tests.Services;

public sealed class GroupServiceTests
{
    private readonly IGroupApi _groupApi;
    private readonly IEventApiClient _apiClient;
    private readonly GroupService _service;

    public GroupServiceTests()
    {
        _groupApi = Substitute.For<IGroupApi>();
        _apiClient = Substitute.For<IEventApiClient>();
        var logger = Substitute.For<ILogger<GroupService>>();
        _service = new GroupService(_groupApi, _apiClient, logger);
    }

    [Test]
    public async Task GetMyGroupsAsync_UsesRefitApiAndMapsHalItems()
    {
        var groupId = Guid.NewGuid();
        using var handler = new RecordingHandler(request => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent($$"""
            {
              "_embedded": {
                "items": [
                  {
                    "id": "{{groupId}}",
                    "fullName": "Community Group",
                    "currentUserRole": 2
                  }
                ]
              }
            }
            """, Encoding.UTF8, "application/json")
        });

        var service = CreateRefitBackedService(handler);

        var groups = await service.GetMyGroupsAsync();

        await Assert.That(handler.Requests.Single().RequestUri?.PathAndQuery)
            .IsEqualTo("/api/Group/my?pageNumber=1&pageSize=100");
        await Assert.That(groups.Count).IsEqualTo(1);
        var group = groups.Single();
        await Assert.That(group.Id).IsEqualTo(groupId);
        await Assert.That(group.FullName).IsEqualTo("Community Group");
        await Assert.That(group.CurrentUserRole).IsEqualTo(2);
    }

    [Test]
    public async Task GetGroupDetailsAsync_UsesRefitApiAndPreservesHalLinks()
    {
        var groupId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        using var handler = new RecordingHandler(request => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent($$"""
            {
              "id": "{{groupId}}",
              "fullName": "Community Group",
              "description": "Group description",
              "actorId": "{{actorId}}",
              "actorBackgroundColor": "#123456",
              "actorBackgroundEffect": "gradient",
              "actorBannerColor": "#654321",
              "actorBannerPictureUri": "/media/banner.png",
              "actorProfilePictureUri": "/media/profile.png",
              "_links": {
                "edit": { "href": "/api/Group/{{groupId}}", "method": "PUT" },
                "delete": { "href": "/api/Group/{{groupId}}", "method": "DELETE" }
              }
            }
            """, Encoding.UTF8, "application/json")
        });

        var service = CreateRefitBackedService(handler);

        var group = await service.GetGroupDetailsAsync(groupId);

        await Assert.That(handler.Requests.Single().RequestUri?.PathAndQuery)
            .IsEqualTo($"/api/Group/{groupId}");
        await Assert.That(group).IsNotNull();
        await Assert.That(group!.Id).IsEqualTo(groupId);
        await Assert.That(group.ActorId).IsEqualTo(actorId);
        await Assert.That(group.HasHalLink("edit")).IsTrue();
        await Assert.That(group.HasHalLink("delete")).IsTrue();
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
            .ThrowsAsync(new Explore.Blazor.Client.Clients.ApiException("API Error", 500, null, null, null));

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

    private GroupService CreateRefitBackedService(HttpMessageHandler handler)
    {
        var client = new HttpClient(handler, disposeHandler: false)
        {
            BaseAddress = new Uri("https://client.test")
        };
        var groupApi = RestService.For<IGroupApi>(client);
        return new GroupService(groupApi, _apiClient, Substitute.For<ILogger<GroupService>>());
    }

    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(responder(request));
        }
    }
}
