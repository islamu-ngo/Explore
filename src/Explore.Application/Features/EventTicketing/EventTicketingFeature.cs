// ABOUTME: CQRS requests, DTOs, and handlers for platform-managed event ticket authoring.
// ABOUTME: Uses the ticket catalog aggregate for draft mutation and generic not-found boundaries.

using Explore.Application.Authorization;
using Explore.Application.Caching;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace Explore.Application.Features.EventTicketing;

public sealed class EventTicketCatalogManagementDto
{
    public Guid CatalogId { get; init; }
    public int VersionNumber { get; init; }
    public string CurrencyCode { get; init; } = string.Empty;
    public int StatusId { get; init; }
    public IReadOnlyList<EventTicketTypeDto> TicketTypes { get; init; } = [];
    public IReadOnlyList<EventCapacityPoolDto> CapacityPools { get; init; } = [];
}

public sealed class EventTicketTypeDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public int TicketPricingModeId { get; init; }
    public long? FixedPriceMinor { get; init; }
    public long? MinimumPriceMinor { get; init; }
    public long? SuggestedPriceMinor { get; init; }
    public int ParticipantDataCollectionModeId { get; init; }
    public Guid? CapacityPoolId { get; init; }
    public int? MinimumAge { get; init; }
    public int? MaximumAge { get; init; }
    public bool RequiresGuardian { get; init; }
    public bool RequiresApproval { get; init; }
    public int? PerOrderLimit { get; init; }
    public int? PerAccountLimit { get; init; }
    public int? PerVerifiedContactLimit { get; init; }
    public int? PerBookingPartyLimit { get; init; }
    public IReadOnlyList<TicketTypeEntitlementDto> Entitlements { get; init; } = [];
}

public sealed class TicketTypeEntitlementDto
{
    public int EntitlementScopeTypeId { get; init; }
    public Guid? EventDayId { get; init; }
    public Guid? EventSessionId { get; init; }
    public int IncludedQuantity { get; init; }
    public int EntitlementSelectionRuleId { get; init; }
}

public sealed class EventCapacityPoolDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public int? MaximumQuantity { get; init; }
    public int HoldDurationSeconds { get; init; }
    public int CapacityOversellPolicyId { get; init; }
    public bool IsActive { get; init; }
}

[AuthorizeResource(ResourceKinds.EventTicketType, AuthorizationActions.Events.ManageTickets)]
public sealed record GetEventTicketCatalogManagementQuery(Guid EventId)
    : IRequest<EventTicketCatalogManagementDto?>, ISecureRequest
{
    string? ISecureRequest.ResourceId => EventId.ToString();
    IDictionary<string, object>? ISecureRequest.ResourceAttributes => new Dictionary<string, object> { ["eventId"] = EventId.ToString() };
}

public abstract class TicketingCommand : ISecureRequest
{
    public Guid EventId { get; init; }
    string? ISecureRequest.ResourceId => EventId.ToString();
    IDictionary<string, object>? ISecureRequest.ResourceAttributes => new Dictionary<string, object> { ["eventId"] = EventId.ToString() };
}

[AuthorizeResource(ResourceKinds.EventTicketType, AuthorizationActions.Events.ManageTickets)]
public sealed class CreateEventTicketCatalogDraftCommand : TicketingCommand, IRequest<BaseCommandResponse<Guid>>
{
    public required string CurrencyCode { get; init; }
}
[AuthorizeResource(ResourceKinds.EventTicketType, AuthorizationActions.Events.ManageTickets)]
public sealed class CloneEventTicketCatalogDraftCommand : TicketingCommand, IRequest<BaseCommandResponse<Guid>>;
[AuthorizeResource(ResourceKinds.EventTicketType, AuthorizationActions.Events.ManageTickets)]
public sealed class CreateEventTicketTypeCommand : TicketingCommand, IRequest<BaseCommandResponse<Guid>>
{
    public required EventTicketTypeDto TicketType { get; init; }
}
[AuthorizeResource(ResourceKinds.EventTicketType, AuthorizationActions.Events.ManageTickets)]
public sealed class UpdateEventTicketTypeCommand : TicketingCommand, IRequest<BaseCommandResponse<Guid>>
{
    public Guid TicketTypeId { get; init; }
    public required EventTicketTypeDto TicketType { get; init; }
}
[AuthorizeResource(ResourceKinds.EventTicketType, AuthorizationActions.Events.ManageTickets)]
public sealed class DeleteEventTicketTypeCommand : TicketingCommand, IRequest<BaseCommandResponse<Guid>>
{
    public Guid TicketTypeId { get; init; }
}
[AuthorizeResource(ResourceKinds.EventTicketType, AuthorizationActions.Events.ManageTickets)]
public sealed class CreateEventCapacityPoolCommand : TicketingCommand, IRequest<BaseCommandResponse<Guid>>
{
    public required EventCapacityPoolDto CapacityPool { get; init; }
}
[AuthorizeResource(ResourceKinds.EventTicketType, AuthorizationActions.Events.ManageTickets)]
public sealed class UpdateEventCapacityPoolCommand : TicketingCommand, IRequest<BaseCommandResponse<Guid>>
{
    public Guid CapacityPoolId { get; init; }
    public required EventCapacityPoolDto CapacityPool { get; init; }
}
[AuthorizeResource(ResourceKinds.EventTicketType, AuthorizationActions.Events.ManageTickets)]
public sealed class DeleteEventCapacityPoolCommand : TicketingCommand, IRequest<BaseCommandResponse<Guid>>
{
    public Guid CapacityPoolId { get; init; }
}
[AuthorizeResource(ResourceKinds.EventTicketType, AuthorizationActions.Events.ManageTickets)]
public sealed class PublishEventTicketCatalogCommand : TicketingCommand, IRequest<BaseCommandResponse<Guid>>;

public sealed class EventTicketTypeDtoValidator : AbstractValidator<EventTicketTypeDto>
{
    public EventTicketTypeDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Entitlements).NotEmpty();
    }
}

public sealed class EventCapacityPoolDtoValidator : AbstractValidator<EventCapacityPoolDto>
{
    public EventCapacityPoolDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.HoldDurationSeconds).GreaterThan(0);
    }
}

public sealed class EventTicketingService(IEventRepository events, IEventTicketCatalogRepository catalogs, IEventDayRepository days, IEventSessionRepository sessions, ITenantContext tenant, ICurrentUserService user, HybridCache cache)
{
    public async Task<EventTicketCatalogManagementDto?> Handle(GetEventTicketCatalogManagementQuery request, CancellationToken cancellationToken)
    {
        var catalog = await DraftAsync(request.EventId, cancellationToken)
            ?? await catalogs.GetPublishedCatalogAsync(request.EventId, tenant.TenantId, cancellationToken);
        return catalog is null ? null : Map(catalog, await PoolsAsync(request.EventId));
    }

    public async Task<BaseCommandResponse<Guid>> Handle(CreateEventTicketCatalogDraftCommand request, CancellationToken cancellationToken)
    {
        if (!await PlatformAsync(request.EventId, cancellationToken)
            || await catalogs.GetManagementCatalogAsync(request.EventId, tenant.TenantId, cancellationToken) is not null)
        {
            return Missing(request.EventId);
        }

        try
        {
            var catalog = EventTicketCatalogVersion.Create(tenant.TenantId, request.EventId, request.CurrencyCode, 1);
            await catalogs.AddAsync(catalog, cancellationToken);
            return Ok(catalog.Id, "Ticket catalog draft created.");
        }
        catch (ArgumentException exception)
        {
            return Bad(request.EventId, exception.Message);
        }
    }

    public async Task<BaseCommandResponse<Guid>> Handle(CloneEventTicketCatalogDraftCommand request, CancellationToken cancellationToken)
    {
        var publishedCatalog = await catalogs.GetPublishedCatalogAsync(request.EventId, tenant.TenantId, cancellationToken);
        if (!await PlatformAsync(request.EventId, cancellationToken) || publishedCatalog is null)
        {
            return Missing(request.EventId);
        }

        var managementCatalog = await catalogs.GetManagementCatalogAsync(request.EventId, tenant.TenantId, cancellationToken);
        if (managementCatalog?.TicketCatalogStatusId == (int)TicketCatalogStatusEnum.Draft)
        {
            return Bad(request.EventId, "A ticket catalog draft already exists.");
        }

        try
        {
            var draft = publishedCatalog.CloneToDraft();
            await catalogs.AddAsync(draft, cancellationToken);
            return Ok(draft.Id, "Ticket catalog draft cloned.");
        }
        catch (InvalidOperationException exception)
        {
            return Bad(request.EventId, exception.Message);
        }
    }
    public async Task<BaseCommandResponse<Guid>> Handle(CreateEventTicketTypeCommand r, CancellationToken ct) { var v = await new EventTicketTypeDtoValidator().ValidateAsync(r.TicketType, ct); var c = await DraftAsync(r.EventId, ct); if (c is null) return Missing(r.EventId); if (!v.IsValid) return Bad(r.EventId, v.Errors.Select(x => x.ErrorMessage)); try { var pool = await PoolAsync(r.TicketType.CapacityPoolId, r.EventId, ct); if (r.TicketType.CapacityPoolId.HasValue && pool is null) return Missing(r.EventId); var t = Type(c, r.TicketType); c.AddTicketType(t, pool); await EntitlementsAsync(c, t, r.TicketType.Entitlements, r.EventId); await catalogs.UpdateAsync(c, ct); await Invalidate(r.EventId, ct); return Ok(t.Id, "Ticket type created."); } catch (TicketingNotFoundException) { return Missing(r.EventId); } catch (ArgumentException e) { return Bad(r.EventId, e.Message); } }
    public async Task<BaseCommandResponse<Guid>> Handle(UpdateEventTicketTypeCommand r, CancellationToken ct) { var v = await new EventTicketTypeDtoValidator().ValidateAsync(r.TicketType, ct); if (!v.IsValid) return Bad(r.TicketTypeId, v.Errors.Select(x => x.ErrorMessage)); var c = await DraftAsync(r.EventId, ct); var t = c?.TicketTypes.SingleOrDefault(x => x.Id == r.TicketTypeId && !x.IsDeleted); if (t is null) return Missing(r.TicketTypeId); try { var p = await PoolAsync(r.TicketType.CapacityPoolId, r.EventId, ct); if (r.TicketType.CapacityPoolId.HasValue && p is null) return Missing(r.TicketTypeId); var es = await CreateEntitlementsAsync(t.Id, r.TicketType.Entitlements, r.EventId); c!.UpdateTicketType(t, r.TicketType.Name, (TicketPricingModeEnum)r.TicketType.TicketPricingModeId, r.TicketType.FixedPriceMinor, r.TicketType.MinimumPriceMinor, r.TicketType.SuggestedPriceMinor, (ParticipantDataCollectionModeEnum)r.TicketType.ParticipantDataCollectionModeId, p, r.TicketType.MinimumAge, r.TicketType.MaximumAge, r.TicketType.RequiresGuardian, r.TicketType.RequiresApproval, r.TicketType.PerOrderLimit, r.TicketType.PerAccountLimit, r.TicketType.PerVerifiedContactLimit, r.TicketType.PerBookingPartyLimit, es); await catalogs.UpdateAsync(c, ct); await Invalidate(r.EventId, ct); return Ok(t.Id, "Ticket type updated."); } catch (TicketingNotFoundException) { return Missing(r.TicketTypeId); } catch (ArgumentException e) { return Bad(t.Id, e.Message); } }
    public async Task<BaseCommandResponse<Guid>> Handle(DeleteEventTicketTypeCommand r, CancellationToken ct) { var c = await DraftAsync(r.EventId, ct); var t = c?.TicketTypes.SingleOrDefault(x => x.Id == r.TicketTypeId && !x.IsDeleted); if (t is null) return Missing(r.TicketTypeId); try { c!.DeleteTicketType(t); await catalogs.UpdateAsync(c, ct); await Invalidate(r.EventId, ct); return Ok(t.Id, "Ticket type deleted."); } catch (ArgumentException e) { return Bad(t.Id, e.Message); } }
    public async Task<BaseCommandResponse<Guid>> Handle(CreateEventCapacityPoolCommand r, CancellationToken ct) { var v = await new EventCapacityPoolDtoValidator().ValidateAsync(r.CapacityPool, ct); if (!v.IsValid) return Bad(r.EventId, v.Errors.Select(x => x.ErrorMessage)); if (!await PlatformAsync(r.EventId, ct)) return Missing(r.EventId); try { var p = EventCapacityPool.Create(tenant.TenantId, r.EventId, r.CapacityPool.Name, r.CapacityPool.MaximumQuantity, r.CapacityPool.HoldDurationSeconds, (CapacityOversellPolicyEnum)r.CapacityPool.CapacityOversellPolicyId, r.CapacityPool.IsActive); await catalogs.AddCapacityPoolAsync(p, ct); await Invalidate(r.EventId, ct); return Ok(p.Id, "Capacity pool created."); } catch (ArgumentException e) { return Bad(r.EventId, e.Message); } }
    public async Task<BaseCommandResponse<Guid>> Handle(UpdateEventCapacityPoolCommand r, CancellationToken ct) { var v = await new EventCapacityPoolDtoValidator().ValidateAsync(r.CapacityPool, ct); if (!v.IsValid) return Bad(r.CapacityPoolId, v.Errors.Select(x => x.ErrorMessage)); var p = await PoolAsync(r.CapacityPoolId, r.EventId, ct); if (p is null || !await PlatformAsync(r.EventId, ct)) return Missing(r.CapacityPoolId); try { p.Update(r.CapacityPool.Name, r.CapacityPool.MaximumQuantity, r.CapacityPool.HoldDurationSeconds, (CapacityOversellPolicyEnum)r.CapacityPool.CapacityOversellPolicyId, r.CapacityPool.IsActive); await catalogs.UpdateCapacityPoolAsync(p, ct); await Invalidate(r.EventId, ct); return Ok(p.Id, "Capacity pool updated."); } catch (ArgumentException e) { return Bad(p.Id, e.Message); } }
    public async Task<BaseCommandResponse<Guid>> Handle(DeleteEventCapacityPoolCommand r, CancellationToken ct) { var p = await PoolAsync(r.CapacityPoolId, r.EventId, ct); if (p is null || !await PlatformAsync(r.EventId, ct)) return Missing(r.CapacityPoolId); var c = await DraftAsync(r.EventId, ct); if (c?.TicketTypes.Any(x => !x.IsDeleted && x.CapacityPoolId == p.Id) == true) return Bad(p.Id, "Capacity pool is assigned to an active ticket type."); p.IsDeleted = true; p.DeletedAt = DateTime.UtcNow; p.DeletedBy = user.UserId; await catalogs.UpdateCapacityPoolAsync(p, ct); await Invalidate(r.EventId, ct); return Ok(p.Id, "Capacity pool deleted."); }
    public async Task<BaseCommandResponse<Guid>> Handle(PublishEventTicketCatalogCommand request, CancellationToken cancellationToken)
    {
        var draft = await DraftAsync(request.EventId, cancellationToken);
        if (draft is null)
        {
            return Missing(request.EventId);
        }

        try
        {
            await catalogs.PublishDraftReplacingCurrentAsync(draft, request.EventId, tenant.TenantId, cancellationToken);
            await Invalidate(request.EventId, cancellationToken);
            return Ok(draft.Id, "Ticket catalog published.");
        }
        catch (ArgumentException exception)
        {
            return Bad(draft.Id, exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Bad(draft.Id, exception.Message);
        }
    }

    private async Task<EventTicketCatalogVersion?> DraftAsync(Guid eventId, CancellationToken cancellationToken)
    {
        if (!await PlatformAsync(eventId, cancellationToken))
        {
            return null;
        }

        var catalog = await catalogs.GetManagementCatalogAsync(eventId, tenant.TenantId, cancellationToken);
        return catalog?.TicketCatalogStatusId == (int)TicketCatalogStatusEnum.Draft ? catalog : null;
    }

    private async Task<bool> PlatformAsync(Guid eventId, CancellationToken cancellationToken)
    {
        var eventTarget = await events.GetAuthorizationTargetByIdAsync(eventId, cancellationToken);
        return eventTarget?.TenantId == tenant.TenantId
            && eventTarget.ParticipationConfiguration?.ParticipationHandlingModeId == (int)ParticipationHandlingModeEnum.PlatformManaged;
    }
    private async Task<IReadOnlyList<EventCapacityPool>> PoolsAsync(Guid eventId)
    {
        return (await events.GetEventWithDetails(eventId))?.CapacityPools.Where(pool => !pool.IsDeleted).ToArray() ?? [];
    }

    private Task<EventCapacityPool?> PoolAsync(Guid? id, Guid eventId, CancellationToken cancellationToken)
    {
        return id.HasValue
            ? catalogs.GetCapacityPoolByIdEventAndTenantAsync(id.Value, eventId, tenant.TenantId, cancellationToken)
            : Task.FromResult<EventCapacityPool?>(null);
    }

    private static EventTicketType Type(EventTicketCatalogVersion catalog, EventTicketTypeDto dto)
    {
        return EventTicketType.Create(
            catalog.TenantId,
            catalog.Id,
            dto.Name,
            catalog.CurrencyCode,
            (TicketPricingModeEnum)dto.TicketPricingModeId,
            dto.FixedPriceMinor,
            dto.MinimumPriceMinor,
            dto.SuggestedPriceMinor,
            (ParticipantDataCollectionModeEnum)dto.ParticipantDataCollectionModeId,
            dto.CapacityPoolId,
            dto.MinimumAge,
            dto.MaximumAge,
            dto.RequiresGuardian,
            dto.RequiresApproval,
            dto.PerOrderLimit,
            dto.PerAccountLimit,
            dto.PerVerifiedContactLimit,
            dto.PerBookingPartyLimit);
    }

    private async Task EntitlementsAsync(EventTicketCatalogVersion catalog, EventTicketType ticketType, IReadOnlyList<TicketTypeEntitlementDto> values, Guid eventId)
    {
        foreach (var entitlement in await CreateEntitlementsAsync(ticketType.Id, values, eventId))
        {
            catalog.AddEntitlement(ticketType, entitlement);
        }
    }
    private async Task<IReadOnlyList<TicketTypeEntitlement>> CreateEntitlementsAsync(Guid ticketId, IReadOnlyList<TicketTypeEntitlementDto> values, Guid eventId) => await Task.WhenAll(values.Select(async d => (EntitlementScopeTypeEnum)d.EntitlementScopeTypeId switch { EntitlementScopeTypeEnum.Event => TicketTypeEntitlement.CreateForEvent(ticketId, tenant.TenantId, eventId, d.IncludedQuantity), EntitlementScopeTypeEnum.EventDay when d.EventDayId.HasValue => CreateDayEntitlement(ticketId, await days.GetById(d.EventDayId.Value), d, eventId), EntitlementScopeTypeEnum.EventSession when d.EventSessionId.HasValue => CreateSessionEntitlement(ticketId, await sessions.GetById(d.EventSessionId.Value), d, eventId), _ => throw new ArgumentException("Entitlement scope is invalid.") }));
    private TicketTypeEntitlement CreateDayEntitlement(Guid ticketId, EventDay? day, TicketTypeEntitlementDto dto, Guid eventId) { if (day?.EventId != eventId || day.TenantId != tenant.TenantId) throw new TicketingNotFoundException(); return TicketTypeEntitlement.CreateForEventDay(ticketId, day, dto.IncludedQuantity, (EntitlementSelectionRuleEnum)dto.EntitlementSelectionRuleId); }
    private TicketTypeEntitlement CreateSessionEntitlement(Guid ticketId, EventSession? session, TicketTypeEntitlementDto dto, Guid eventId) { if (session?.EventId != eventId || session.TenantId != tenant.TenantId) throw new TicketingNotFoundException(); return TicketTypeEntitlement.CreateForEventSession(ticketId, session, dto.IncludedQuantity, (EntitlementSelectionRuleEnum)dto.EntitlementSelectionRuleId); }
    private static EventTicketCatalogManagementDto Map(EventTicketCatalogVersion c, IReadOnlyList<EventCapacityPool> pools) => new() { CatalogId = c.Id, VersionNumber = c.VersionNumber, CurrencyCode = c.CurrencyCode, StatusId = c.TicketCatalogStatusId, TicketTypes = c.TicketTypes.Where(t => !t.IsDeleted).Select(t => new EventTicketTypeDto { Id = t.Id, Name = t.Name, TicketPricingModeId = t.TicketPricingModeId, FixedPriceMinor = t.FixedPriceMinor, MinimumPriceMinor = t.MinimumPriceMinor, SuggestedPriceMinor = t.SuggestedPriceMinor, ParticipantDataCollectionModeId = t.ParticipantDataCollectionModeId, CapacityPoolId = t.CapacityPoolId, MinimumAge = t.MinimumAge, MaximumAge = t.MaximumAge, RequiresGuardian = t.RequiresGuardian, RequiresApproval = t.RequiresApproval, PerOrderLimit = t.PerOrderLimit, PerAccountLimit = t.PerAccountLimit, PerVerifiedContactLimit = t.PerVerifiedContactLimit, PerBookingPartyLimit = t.PerBookingPartyLimit, Entitlements = t.Entitlements.Select(e => new TicketTypeEntitlementDto { EntitlementScopeTypeId = e.EntitlementScopeTypeId, EventDayId = e.EventDayId, EventSessionId = e.EventSessionId, IncludedQuantity = e.IncludedQuantity, EntitlementSelectionRuleId = e.EntitlementSelectionRuleId }).ToArray() }).ToArray(), CapacityPools = pools.Select(p => new EventCapacityPoolDto { Id = p.Id, Name = p.Name, MaximumQuantity = p.MaximumQuantity, HoldDurationSeconds = p.HoldDurationSeconds, CapacityOversellPolicyId = p.CapacityOversellPolicyId, IsActive = p.IsActive }).ToArray() };
    private async Task Invalidate(Guid id, CancellationToken ct) => await cache.RemoveAsync($"event:detail:{id}", ct);
    private static BaseCommandResponse<Guid> Ok(Guid id, string message) => new() { Id = id, Success = true, Message = message };
    private static BaseCommandResponse<Guid> Missing(Guid id) => new() { Id = id, Success = false, FailureCode = "event_ticketing_not_found", Message = "Ticketing configuration was not found.", Errors = ["Ticketing configuration was not found."] };
    private static BaseCommandResponse<Guid> Bad(Guid id, string error) => Bad(id, [error]);
    private static BaseCommandResponse<Guid> Bad(Guid id, IEnumerable<string> errors) => new() { Id = id, Success = false, FailureCode = "event_ticketing_validation_failed", Message = "Ticketing configuration is invalid.", Errors = errors.ToList() };
    private sealed class TicketingNotFoundException : Exception;
}
