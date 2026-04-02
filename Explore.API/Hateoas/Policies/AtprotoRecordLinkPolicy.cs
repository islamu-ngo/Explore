namespace Explore.API.Hateoas.Policies;

using System.Security.Claims;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.AtprotoRecord;
using Explore.Application.Hateoas;

/// <summary>
/// Link policy for AtprotoRecordDto (detail view).
/// Provides links for ATProto record operations (federation).
/// </summary>
public sealed class AtprotoRecordDetailLinkPolicy : ILinkPolicy<AtprotoRecordDto>
{
    /// <inheritdoc />
    public IEnumerable<LinkDefinition> GetLinks(AtprotoRecordDto dto, ClaimsPrincipal? user)
    {
        // Self link
        yield return new LinkDefinition(
            LinkRelations.Self,
            RouteNames.GetAtprotoRecordById,
            new { id = dto.Id },
            "GET",
            $"ATProto record: {dto.Collection}");

        // Collection link
        yield return new LinkDefinition(
            LinkRelations.Collection,
            RouteNames.GetAtprotoRecords,
            null,
            "GET",
            "All ATProto records");

        // By URI link (ATProto canonical link)
        if (!string.IsNullOrEmpty(dto.Uri))
        {
            yield return new LinkDefinition(
                "by-uri",
                RouteNames.GetAtprotoRecordByUri,
                new { uri = dto.Uri },
                "GET",
                "Record by AT URI");
        }

        // DID link (indexed DID reference)
        yield return new LinkDefinition(
            "did",
            RouteNames.GetIndexedDidByDid,
            new { did = dto.Did },
            "GET",
            $"DID: {dto.Did}");

        // Delete link - requires authentication
        yield return new LinkDefinition(
            "delete",
            RouteNames.DeleteAtprotoRecord,
            new { id = dto.Id },
            "DELETE",
            "Delete record",
            RequiresAuth: true)
            .RequirePermission(AuthorizationActions.Delete, ResourceDescriptors.AtprotoRecord, dto);
    }
}

/// <summary>
/// Link policy for AtprotoRecordListDto (collection items).
/// </summary>
public sealed class AtprotoRecordCollectionLinkPolicy : ICollectionLinkPolicy<AtprotoRecordListDto>
{
    /// <inheritdoc />
    public IEnumerable<LinkDefinition> GetItemLinks(AtprotoRecordListDto dto, ClaimsPrincipal? user)
    {
        // Self link for item
        yield return new LinkDefinition(
            LinkRelations.Self,
            RouteNames.GetAtprotoRecordById,
            new { id = dto.Id },
            "GET",
            $"ATProto record: {dto.Collection}");

        // DID link
        yield return new LinkDefinition(
            "did",
            RouteNames.GetIndexedDidByDid,
            new { did = dto.Did },
            "GET",
            dto.Did);
    }

    /// <inheritdoc />
    public IEnumerable<LinkDefinition> GetCollectionLinks(ClaimsPrincipal? user)
    {
        // Create link - requires authentication
        yield return new LinkDefinition(
            "create",
            RouteNames.CreateAtprotoRecord,
            null,
            "POST",
            "Create ATProto record",
            RequiresAuth: true)
            .RequirePermission(AuthorizationActions.Create, typeof(AtprotoRecordDto), "atproto_record");
    }
}
