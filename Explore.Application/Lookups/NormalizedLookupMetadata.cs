// ABOUTME: Canonical lookup metadata for enum-backed lookup rows exposed through API DTOs.
// ABOUTME: Mirrors LookupTableSeeder stable IDs/codes so handlers can map without loading navigations.

using Explore.Domain.Enums;
using ExternalApiKeyOwnerTypeEnum = Explore.Domain.Enums.ExternalApiKeyOwnerType;

namespace Explore.Application.Lookups;

public static class NormalizedLookupMetadata
{
    public static LookupReference RoleScope(int id)
    {
        return id switch
        {
            (int)RoleScopeEnum.Platform => new(id, "PLATFORM", "Platform"),
            (int)RoleScopeEnum.Tenant => new(id, "TENANT", "Tenant"),
            (int)RoleScopeEnum.Organization => new(id, "ORGANIZATION", "Organization"),
            (int)RoleScopeEnum.Group => new(id, "GROUP", "Group"),
            (int)RoleScopeEnum.Event => new(id, "EVENT", "Event"),
            _ => Unknown(id)
        };
    }

    public static LookupReference SettingValueType(int id)
    {
        return id switch
        {
            (int)Explore.Domain.SettingValueType.String => new(id, "STRING", "String"),
            (int)Explore.Domain.SettingValueType.Integer => new(id, "INTEGER", "Integer"),
            (int)Explore.Domain.SettingValueType.Boolean => new(id, "BOOLEAN", "Boolean"),
            (int)Explore.Domain.SettingValueType.Decimal => new(id, "DECIMAL", "Decimal"),
            (int)Explore.Domain.SettingValueType.Json => new(id, "JSON", "JSON"),
            (int)Explore.Domain.SettingValueType.DateTime => new(id, "DATE_TIME", "Date/Time"),
            _ => Unknown(id)
        };
    }

    public static LookupReference ExternalApiKeyOwnerType(int id)
    {
        return id switch
        {
            (int)ExternalApiKeyOwnerTypeEnum.User => new(id, "USER", "User"),
            (int)ExternalApiKeyOwnerTypeEnum.Organization => new(id, "ORGANIZATION", "Organization"),
            (int)ExternalApiKeyOwnerTypeEnum.Group => new(id, "GROUP", "Group"),
            (int)ExternalApiKeyOwnerTypeEnum.Tenant => new(id, "TENANT", "Tenant"),
            (int)ExternalApiKeyOwnerTypeEnum.InstanceAdmin => new(id, "INSTANCE_ADMIN", "Instance Admin"),
            _ => Unknown(id)
        };
    }

    public static LookupReference ExternalApiKeyStatus(int id)
    {
        return id switch
        {
            (int)ExternalApiKeyStatusEnum.Active => new(id, "ACTIVE", "Active"),
            (int)ExternalApiKeyStatusEnum.Revoked => new(id, "REVOKED", "Revoked"),
            (int)ExternalApiKeyStatusEnum.Expired => new(id, "EXPIRED", "Expired"),
            (int)ExternalApiKeyStatusEnum.Suspended => new(id, "SUSPENDED", "Suspended"),
            (int)ExternalApiKeyStatusEnum.PendingRotation => new(id, "PENDING_ROTATION", "Pending Rotation"),
            _ => Unknown(id)
        };
    }

    public static LookupReference ExternalApiKeyCreditPeriod(int id)
    {
        return id switch
        {
            (int)ExternalApiKeyCreditPeriodEnum.None => new(id, "NONE", "None"),
            (int)ExternalApiKeyCreditPeriodEnum.Daily => new(id, "DAILY", "Daily"),
            (int)ExternalApiKeyCreditPeriodEnum.Weekly => new(id, "WEEKLY", "Weekly"),
            (int)ExternalApiKeyCreditPeriodEnum.Monthly => new(id, "MONTHLY", "Monthly"),
            (int)ExternalApiKeyCreditPeriodEnum.Yearly => new(id, "YEARLY", "Yearly"),
            _ => Unknown(id)
        };
    }

    public static bool IsRoleScopeId(int id)
    {
        return Enum.IsDefined(typeof(RoleScopeEnum), id);
    }

    public static bool IsExternalApiKeyOwnerTypeId(int id)
    {
        return Enum.IsDefined(typeof(ExternalApiKeyOwnerTypeEnum), id);
    }

    private static LookupReference Unknown(int id)
    {
        return new LookupReference(id, "UNKNOWN", "Unknown");
    }
}
