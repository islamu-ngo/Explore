// ABOUTME: Unit tests for converting untrusted AI CreateEventDraft proposals into safe draft DTOs.
// ABOUTME: Verifies privileged fields, unknown fields, validation bounds, and owner-scope allow-lists are enforced.

using Explore.Application.Features.AiAssistant.Actions;

namespace Event.Application.UnitTests.Features.AiAssistant.Actions;

public sealed class CreateEventDraftAiActionMapperTests
{
    [Test]
    public async Task Map_WhenPayloadIsMinimal_ReturnsDraftRequestWithoutProgramGraph()
    {
        var categoryId1 = Guid.CreateVersion7();
        var categoryId2 = Guid.CreateVersion7();
        var tagId = Guid.CreateVersion7();

        var result = new CreateEventDraftAiActionMapper().Map(
            $$"""
              {
                "title": "  Community Iftar  ",
                "description": "Evening meal",
                "categoryIds": ["{{categoryId1}}", "{{categoryId1}}", "{{categoryId2}}"],
                "tagIds": ["{{tagId}}", "{{tagId}}"]
              }
              """);

        await Assert.That(result.Succeeded).IsTrue();
        await Assert.That(result.Draft).IsNotNull();
        await Assert.That(result.Draft!.Title).IsEqualTo("Community Iftar");
        await Assert.That(result.Draft.Description).IsEqualTo("Evening meal");
        await Assert.That(result.Draft.CategoryIds).IsEquivalentTo([categoryId1, categoryId2]);
        await Assert.That(result.Draft.TagIds).IsEquivalentTo([tagId]);

        var createRequest = result.Draft.ToCreateEventRequest();
        await Assert.That(createRequest.EventStatusId).IsEqualTo(1);
        await Assert.That(createRequest.Sessions).IsEmpty();
        await Assert.That(createRequest.Days).IsEmpty();
        await Assert.That(createRequest.Rooms).IsEmpty();
        await Assert.That(createRequest.AgendaItems).IsEmpty();
    }

    [Test]
    public async Task Map_WhenTitleIsMissing_ReturnsMissingTitleFailure()
    {
        var result = new CreateEventDraftAiActionMapper().Map("{\"description\":\"No title\"}");

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("missing_title");
    }

    [Test]
    public async Task Map_WhenTitleIsTooLong_ReturnsFieldTooLongFailure()
    {
        var result = new CreateEventDraftAiActionMapper().Map($"{{\"title\":\"{new string('A', 201)}\"}}");

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("field_too_long");
    }

    [Test]
    [Arguments("tenantId")]
    [Arguments("eventStatusId")]
    [Arguments("sessions")]
    [Arguments("days")]
    [Arguments("rooms")]
    [Arguments("agendaItems")]
    [Arguments("roleAssignments")]
    public async Task Map_WhenPayloadContainsPrivilegedField_ReturnsUnsupportedPayloadFieldFailure(string fieldName)
    {
        var result = new CreateEventDraftAiActionMapper().Map($"{{\"title\":\"Draft\",\"{fieldName}\":\"not allowed\"}}");

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("unsupported_payload_field");
    }

    [Test]
    public async Task Map_WhenPayloadContainsUnknownField_ReturnsUnsupportedPayloadFieldFailure()
    {
        var result = new CreateEventDraftAiActionMapper().Map("{\"title\":\"Draft\",\"unexpected\":true}");

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("unsupported_payload_field");
    }

    [Test]
    public async Task Map_WhenOrganizationAndGroupAreBothSet_ReturnsConflictingOwnerScopeFailure()
    {
        var organizationId = Guid.CreateVersion7();
        var groupId = Guid.CreateVersion7();
        var result = new CreateEventDraftAiActionMapper().Map(
            $"{{\"title\":\"Draft\",\"organizationId\":\"{organizationId}\",\"groupId\":\"{groupId}\"}}",
            new CreateEventDraftAiActionMappingContext(
                new HashSet<Guid> { organizationId },
                new HashSet<Guid> { groupId }));

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("conflicting_owner_scope");
    }

    [Test]
    public async Task Map_WhenOrganizationIsNotAllowed_ReturnsInvalidOrganizationScopeFailure()
    {
        var organizationId = Guid.CreateVersion7();
        var result = new CreateEventDraftAiActionMapper().Map(
            $"{{\"title\":\"Draft\",\"organizationId\":\"{organizationId}\"}}");

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("invalid_organization_scope");
    }

    [Test]
    public async Task Map_WhenOrganizationIsAllowed_MapsOrganizationId()
    {
        var organizationId = Guid.CreateVersion7();
        var result = new CreateEventDraftAiActionMapper().Map(
            $"{{\"title\":\"Draft\",\"organizationId\":\"{organizationId}\"}}",
            new CreateEventDraftAiActionMappingContext(
                new HashSet<Guid> { organizationId },
                new HashSet<Guid>()));

        await Assert.That(result.Succeeded).IsTrue();
        await Assert.That(result.Draft).IsNotNull();
        await Assert.That(result.Draft!.OrganizationId).IsEqualTo(organizationId);
    }

    [Test]
    public async Task Map_WhenGroupIsNotAllowed_ReturnsInvalidGroupScopeFailure()
    {
        var groupId = Guid.CreateVersion7();
        var result = new CreateEventDraftAiActionMapper().Map(
            $"{{\"title\":\"Draft\",\"groupId\":\"{groupId}\"}}");

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("invalid_group_scope");
    }

    [Test]
    public async Task Map_WhenPriceIsNegative_ReturnsInvalidNumericValueFailure()
    {
        var result = new CreateEventDraftAiActionMapper().Map("{\"title\":\"Draft\",\"price\":-1}");

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("invalid_numeric_value");
    }
}
