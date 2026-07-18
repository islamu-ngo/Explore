// ABOUTME: Read-only HAL link policies for public globally indexed AT Protocol records.
// ABOUTME: Exposes public navigation while withholding all direct mutation affordances.

namespace Explore.API.Hateoas.Policies;

using System.Security.Claims;
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
            RouteNames.GetAtprotoRecordEntryById,
            new { id = dto.Id },
            "GET",
            $"ATProto record: {dto.Collection}");

        // Collection link
        yield return new LinkDefinition(
            LinkRelations.Collection,
            RouteNames.GetAtprotoRecordEntries,
            null,
            "GET",
            "All ATProto records");

        // DID link (indexed DID reference)
        yield return new LinkDefinition(
            "did",
            RouteNames.GetIndexedDidByDid,
            new { did = dto.Did },
            "GET",
            $"DID: {dto.Did}");

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
            RouteNames.GetAtprotoRecordEntryById,
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
        yield break;
    }
}
