// ABOUTME: Typed presentation state parsed from the generated extension-data ticket catalog HAL resource.
// ABOUTME: Fails closed on malformed data and binds embedded edit/delete affordances to exact item identifiers.

using System.Text.Json;
using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Pages.Studio;

public sealed record EventTicketCatalogState(
    Guid EventId,
    Guid? CatalogId,
    int? VersionNumber,
    string CurrencyCode,
    int? StatusId,
    string? StatusCode,
    string? StatusName,
    IReadOnlyList<EventTicketTypeState> TicketTypes,
    IReadOnlyList<EventCapacityPoolState> CapacityPools,
    IReadOnlyDictionary<string, HalLink> Links)
{
    public bool HasLink(string relation) => Links.ContainsKey(relation);

    public static bool TryParse(
        HalResourceOfEventTicketCatalogManagementDto resource,
        out EventTicketCatalogState? state)
    {
        state = null;
        try
        {
            JsonElement root = JsonSerializer.SerializeToElement(resource.AdditionalProperties);
            if (!TryRequiredGuid(root, "eventId", out Guid eventId)
                || !TryRequiredString(root, "currencyCode", out string currencyCode)
                || !TryLinks(root, required: true, out Dictionary<string, HalLink> links)
                || !links.ContainsKey("self")
                || !root.TryGetProperty("_embedded", out JsonElement embedded)
                || embedded.ValueKind != JsonValueKind.Object
                || !TryTicketTypes(embedded, out IReadOnlyList<EventTicketTypeState> ticketTypes)
                || !TryCapacityPools(embedded, out IReadOnlyList<EventCapacityPoolState> capacityPools))
            {
                return false;
            }

            state = new EventTicketCatalogState(
                eventId,
                OptionalGuid(root, "catalogId"),
                OptionalInt(root, "versionNumber"),
                currencyCode,
                OptionalInt(root, "statusId"),
                OptionalString(root, "statusCode"),
                OptionalString(root, "statusName"),
                ticketTypes,
                capacityPools,
                links);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (NotSupportedException)
        {
            return false;
        }
    }

    private static bool TryTicketTypes(
        JsonElement embedded,
        out IReadOnlyList<EventTicketTypeState> states)
    {
        states = [];
        if (!embedded.TryGetProperty("ticket-types", out JsonElement items)
            || items.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        var parsed = new List<EventTicketTypeState>();
        foreach (JsonElement item in items.EnumerateArray())
        {
            if (!TryTicketType(item, out EventTicketTypeState? state))
            {
                return false;
            }

            parsed.Add(state!);
        }

        states = parsed;
        return true;
    }

    private static bool TryTicketType(JsonElement item, out EventTicketTypeState? state)
    {
        state = null;
        if (item.ValueKind != JsonValueKind.Object
            || !TryRequiredGuid(item, "id", out Guid id)
            || !TryRequiredString(item, "name", out string name)
            || !TryRequiredInt(item, "ticketPricingModeId", out int pricingModeId)
            || !TryRequiredInt(item, "participantDataCollectionModeId", out int dataCollectionModeId)
            || !TryEntitlements(item, out IReadOnlyList<TicketTypeEntitlementState> entitlements)
            || !TryLinks(item, required: false, out Dictionary<string, HalLink> links))
        {
            return false;
        }

        links = FilterItemLinks(links, id);
        state = new EventTicketTypeState(
            id,
            name,
            pricingModeId,
            OptionalString(item, "ticketPricingModeCode"),
            OptionalString(item, "ticketPricingModeName"),
            OptionalLong(item, "fixedPriceMinor"),
            OptionalLong(item, "minimumPriceMinor"),
            OptionalLong(item, "suggestedPriceMinor"),
            dataCollectionModeId,
            OptionalString(item, "participantDataCollectionModeCode"),
            OptionalString(item, "participantDataCollectionModeName"),
            OptionalGuid(item, "capacityPoolId"),
            OptionalInt(item, "minimumAge"),
            OptionalInt(item, "maximumAge"),
            OptionalBool(item, "requiresGuardian"),
            OptionalBool(item, "requiresApproval"),
            OptionalInt(item, "perOrderLimit"),
            OptionalInt(item, "perAccountLimit"),
            OptionalInt(item, "perVerifiedContactLimit"),
            OptionalInt(item, "perBookingPartyLimit"),
            entitlements,
            links);
        return true;
    }

    private static bool TryEntitlements(
        JsonElement item,
        out IReadOnlyList<TicketTypeEntitlementState> states)
    {
        states = [];
        if (!item.TryGetProperty("entitlements", out JsonElement entitlements)
            || entitlements.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        var parsed = new List<TicketTypeEntitlementState>();
        foreach (JsonElement entitlement in entitlements.EnumerateArray())
        {
            if (entitlement.ValueKind != JsonValueKind.Object
                || !TryRequiredInt(entitlement, "entitlementScopeTypeId", out int scopeTypeId)
                || !TryRequiredInt(entitlement, "includedQuantity", out int includedQuantity)
                || !TryRequiredInt(entitlement, "entitlementSelectionRuleId", out int selectionRuleId))
            {
                return false;
            }

            parsed.Add(new TicketTypeEntitlementState(
                scopeTypeId,
                OptionalString(entitlement, "entitlementScopeTypeCode"),
                OptionalString(entitlement, "entitlementScopeTypeName"),
                OptionalGuid(entitlement, "eventDayId"),
                OptionalGuid(entitlement, "eventSessionId"),
                includedQuantity,
                selectionRuleId,
                OptionalString(entitlement, "entitlementSelectionRuleCode"),
                OptionalString(entitlement, "entitlementSelectionRuleName")));
        }

        states = parsed;
        return parsed.Count > 0;
    }

    private static bool TryCapacityPools(
        JsonElement embedded,
        out IReadOnlyList<EventCapacityPoolState> states)
    {
        states = [];
        if (!embedded.TryGetProperty("capacity-pools", out JsonElement items)
            || items.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        var parsed = new List<EventCapacityPoolState>();
        foreach (JsonElement item in items.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object
                || !TryRequiredGuid(item, "id", out Guid id)
                 || !TryRequiredString(item, "name", out string name)
                 || !TryRequiredInt(item, "holdDurationSeconds", out int holdDurationSeconds)
                 || !TryRequiredInt(item, "capacityHoldPolicyId", out int holdPolicyId)
                 || !TryRequiredInt(item, "capacityOversellPolicyId", out int oversellPolicyId)
                || !TryLinks(item, required: false, out Dictionary<string, HalLink> links))
            {
                return false;
            }

            parsed.Add(new EventCapacityPoolState(
                id,
                 name,
                 OptionalInt(item, "maximumQuantity"),
                 holdDurationSeconds,
                 holdPolicyId,
                 OptionalString(item, "capacityHoldPolicyCode"),
                 OptionalString(item, "capacityHoldPolicyName"),
                 oversellPolicyId,
                OptionalString(item, "capacityOversellPolicyCode"),
                OptionalString(item, "capacityOversellPolicyName"),
                OptionalBool(item, "isActive"),
                FilterItemLinks(links, id)));
        }

        states = parsed;
        return true;
    }

    private static bool TryLinks(
        JsonElement resource,
        bool required,
        out Dictionary<string, HalLink> links)
    {
        links = new Dictionary<string, HalLink>(StringComparer.Ordinal);
        if (!resource.TryGetProperty("_links", out JsonElement linkObject))
        {
            return !required;
        }

        if (linkObject.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        foreach (JsonProperty property in linkObject.EnumerateObject())
        {
            HalLink? link = property.Value.Deserialize<HalLink>();
            if (link is null || string.IsNullOrWhiteSpace(link.Href))
            {
                continue;
            }

            links[property.Name] = link;
        }

        return !required || links.Count > 0;
    }

    private static Dictionary<string, HalLink> FilterItemLinks(
        Dictionary<string, HalLink> links,
        Guid itemId) =>
        links
            .Where(pair => pair.Key is not ("edit" or "delete") || LinkTargets(pair.Value, itemId))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);

    private static bool LinkTargets(HalLink link, Guid itemId)
    {
        string path = link.Href!.Split(['?', '#'], 2)[0];
        string? finalSegment = path.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
        return Guid.TryParse(finalSegment, out Guid parsed) && parsed == itemId;
    }

    private static bool TryRequiredGuid(JsonElement source, string name, out Guid value)
    {
        value = Guid.Empty;
        return source.TryGetProperty(name, out JsonElement property)
            && property.ValueKind == JsonValueKind.String
            && property.TryGetGuid(out value)
            && value != Guid.Empty;
    }

    private static bool TryRequiredInt(JsonElement source, string name, out int value)
    {
        value = default;
        return source.TryGetProperty(name, out JsonElement property)
            && property.ValueKind == JsonValueKind.Number
            && property.TryGetInt32(out value);
    }

    private static bool TryRequiredString(JsonElement source, string name, out string value)
    {
        value = string.Empty;
        if (!source.TryGetProperty(name, out JsonElement property)
            || property.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        value = property.GetString() ?? string.Empty;
        return true;
    }

    private static Guid? OptionalGuid(JsonElement source, string name) =>
        source.TryGetProperty(name, out JsonElement property)
        && property.ValueKind == JsonValueKind.String
        && property.TryGetGuid(out Guid value)
        && value != Guid.Empty
            ? value
            : null;

    private static int? OptionalInt(JsonElement source, string name) =>
        source.TryGetProperty(name, out JsonElement property)
        && property.ValueKind == JsonValueKind.Number
        && property.TryGetInt32(out int value)
            ? value
            : null;

    private static long? OptionalLong(JsonElement source, string name) =>
        source.TryGetProperty(name, out JsonElement property)
        && property.ValueKind == JsonValueKind.Number
        && property.TryGetInt64(out long value)
            ? value
            : null;

    private static bool OptionalBool(JsonElement source, string name) =>
        source.TryGetProperty(name, out JsonElement property)
        && property.ValueKind is JsonValueKind.True or JsonValueKind.False
        && property.GetBoolean();

    private static string? OptionalString(JsonElement source, string name) =>
        source.TryGetProperty(name, out JsonElement property)
        && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
}

public sealed record EventTicketTypeState(
    Guid Id,
    string Name,
    int TicketPricingModeId,
    string? TicketPricingModeCode,
    string? TicketPricingModeName,
    long? FixedPriceMinor,
    long? MinimumPriceMinor,
    long? SuggestedPriceMinor,
    int ParticipantDataCollectionModeId,
    string? ParticipantDataCollectionModeCode,
    string? ParticipantDataCollectionModeName,
    Guid? CapacityPoolId,
    int? MinimumAge,
    int? MaximumAge,
    bool RequiresGuardian,
    bool RequiresApproval,
    int? PerOrderLimit,
    int? PerAccountLimit,
    int? PerVerifiedContactLimit,
    int? PerBookingPartyLimit,
    IReadOnlyList<TicketTypeEntitlementState> Entitlements,
    IReadOnlyDictionary<string, HalLink> Links)
{
    public bool HasLink(string relation) => Links.ContainsKey(relation);

    public ManageEventTicketTypeDto ToRequest() => new()
    {
        Name = Name,
        TicketPricingModeId = TicketPricingModeId,
        FixedPriceMinor = FixedPriceMinor,
        MinimumPriceMinor = MinimumPriceMinor,
        SuggestedPriceMinor = SuggestedPriceMinor,
        ParticipantDataCollectionModeId = ParticipantDataCollectionModeId,
        CapacityPoolId = CapacityPoolId,
        MinimumAge = MinimumAge,
        MaximumAge = MaximumAge,
        RequiresGuardian = RequiresGuardian,
        RequiresApproval = RequiresApproval,
        PerOrderLimit = PerOrderLimit,
        PerAccountLimit = PerAccountLimit,
        PerVerifiedContactLimit = PerVerifiedContactLimit,
        PerBookingPartyLimit = PerBookingPartyLimit,
        Entitlements = Entitlements.Select(item => item.ToRequest()).ToArray()
    };
}

public sealed record TicketTypeEntitlementState(
    int EntitlementScopeTypeId,
    string? EntitlementScopeTypeCode,
    string? EntitlementScopeTypeName,
    Guid? EventDayId,
    Guid? EventSessionId,
    int IncludedQuantity,
    int EntitlementSelectionRuleId,
    string? EntitlementSelectionRuleCode,
    string? EntitlementSelectionRuleName)
{
    public ManageTicketTypeEntitlementDto ToRequest() => new()
    {
        EntitlementScopeTypeId = EntitlementScopeTypeId,
        EventDayId = EventDayId,
        EventSessionId = EventSessionId,
        IncludedQuantity = IncludedQuantity,
        EntitlementSelectionRuleId = EntitlementSelectionRuleId
    };
}

public sealed record EventCapacityPoolState(
    Guid Id,
    string Name,
    int? MaximumQuantity,
    int HoldDurationSeconds,
    int CapacityHoldPolicyId,
    string? CapacityHoldPolicyCode,
    string? CapacityHoldPolicyName,
    int CapacityOversellPolicyId,
    string? CapacityOversellPolicyCode,
    string? CapacityOversellPolicyName,
    bool IsActive,
    IReadOnlyDictionary<string, HalLink> Links)
{
    public bool HasLink(string relation) => Links.ContainsKey(relation);

    public ManageEventCapacityPoolDto ToRequest() => new()
    {
        Name = Name,
        MaximumQuantity = MaximumQuantity,
        HoldDurationSeconds = HoldDurationSeconds,
        CapacityHoldPolicyId = CapacityHoldPolicyId,
        CapacityOversellPolicyId = CapacityOversellPolicyId,
        IsActive = IsActive
    };
}
