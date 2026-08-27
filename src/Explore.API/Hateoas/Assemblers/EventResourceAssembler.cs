namespace Explore.API.Hateoas.Assemblers;

using System.Security.Claims;
using Explore.Application.Contracts.Hateoas;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Event;
using Explore.Application.Hateoas;

/// <summary>
/// Resource assembler for Event entities.
/// Converts EventDto and EventListDto to HAL resources with appropriate links.
/// </summary>
public sealed class EventResourceAssembler : ResourceAssemblerBase<EventDto, EventListDto>
{
    public EventResourceAssembler(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<EventDto> detailLinkPolicy,
        ICollectionLinkPolicy<EventListDto> collectionLinkPolicy,
        IEventReportingIntakeGuard intakeGuard)
        : base(linkGenerator, detailLinkPolicy, collectionLinkPolicy)
    {
        _intakeGuard = intakeGuard;
        _collectionLinkPolicy = collectionLinkPolicy;
    }

    private readonly IEventReportingIntakeGuard _intakeGuard;
    private readonly ICollectionLinkPolicy<EventListDto> _collectionLinkPolicy;

    protected override async Task<IReadOnlyList<LinkDefinition>> GetDetailLinkDefinitionsAsync(
        EventDto dto,
        ClaimsPrincipal? user,
        HttpContext httpContext)
    {
        EventDto policyInput = await CreatePolicyInputAsync(dto, httpContext.RequestAborted);
        return await base.GetDetailLinkDefinitionsAsync(policyInput, user, httpContext);
    }

    protected override async Task<IReadOnlyList<LinkDefinition>> GetListItemLinkDefinitionsAsync(
        EventListDto dto,
        ClaimsPrincipal? user,
        HttpContext httpContext)
    {
        EventListDto policyInput = await CreatePolicyInputAsync(dto, httpContext.RequestAborted);
        return await base.GetListItemLinkDefinitionsAsync(policyInput, user, httpContext);
    }

    protected override async Task<IReadOnlyList<IReadOnlyList<LinkDefinition>>> GetCollectionItemLinkDefinitionsAsync(
        IReadOnlyList<EventListDto> items,
        ClaimsPrincipal? user,
        HttpContext httpContext)
    {
        var decisions = new Dictionary<Guid, EventReportingIntakeDecision>();
        foreach (Guid tenantId in items.Select(item => item.TenantId).Where(tenantId => tenantId != Guid.Empty).Distinct())
        {
            decisions[tenantId] = await _intakeGuard.ResolveAsync(tenantId, httpContext.RequestAborted);
        }

        return items
            .Select(item => (IReadOnlyList<LinkDefinition>)GetPolicyLinks(
                CreatePolicyInput(item, decisions.GetValueOrDefault(item.TenantId)),
                user).ToList())
            .ToList();
    }

    private async Task<EventDto> CreatePolicyInputAsync(EventDto dto, CancellationToken cancellationToken)
    {
        EventReportingIntakeDecision? decision = dto.TenantId == Guid.Empty
            ? null
            : await _intakeGuard.ResolveAsync(dto.TenantId, cancellationToken);
        return CreatePolicyInput(dto, decision);
    }

    private async Task<EventListDto> CreatePolicyInputAsync(EventListDto dto, CancellationToken cancellationToken)
    {
        EventReportingIntakeDecision? decision = dto.TenantId == Guid.Empty
            ? null
            : await _intakeGuard.ResolveAsync(dto.TenantId, cancellationToken);
        return CreatePolicyInput(dto, decision);
    }

    private static EventDto CreatePolicyInput(EventDto dto, EventReportingIntakeDecision? decision) => dto with
    {
        IsReportingIntakeEnabled = decision is { TenantResolved: true, IntakeEnabled: true }
    };

    private static EventListDto CreatePolicyInput(EventListDto dto, EventReportingIntakeDecision? decision) => dto with
    {
        IsReportingIntakeEnabled = decision is { TenantResolved: true, IntakeEnabled: true }
    };

    private IEnumerable<LinkDefinition> GetPolicyLinks(EventListDto dto, ClaimsPrincipal? user)
        => _collectionLinkPolicy.GetItemLinks(dto, user);

    /// <summary>
    /// Override to provide embedded actor resource for event details.
    /// </summary>
    protected override Dictionary<string, object>? GetEmbeddedResources(
        EventDto dto,
        Microsoft.AspNetCore.Http.HttpContext httpContext)
    {
        // For now, we don't embed resources to keep responses lean.
        // In the future, we could embed the actor or sessions if requested.
        return null;
    }
}
