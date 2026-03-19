// ABOUTME: Strongly-typed Tenant Delegation setting group resolved via batch loading.
// ABOUTME: Keys align to GovernanceSettingKeys.TenantDelegation for SMTP/storage/analytics lock controls.

namespace Explore.Application.Settings.Groups;

using Explore.Application.Contracts.Infrastructure;
using Explore.Domain.Constants;

public class TenantDelegationSettingGroup : ISettingGroup
{
    public bool LockSmtp { get; private set; } = true;
    public bool LockStorage { get; private set; } = true;
    public bool LockAnalytics { get; private set; } = true;

    public static IEnumerable<string> SettingKeys =>
    [
        GovernanceSettingKeys.TenantDelegation.LockSmtp,
        GovernanceSettingKeys.TenantDelegation.LockStorage,
        GovernanceSettingKeys.TenantDelegation.LockAnalytics
    ];

    public void Populate(IReadOnlyDictionary<string, ResolvedSetting> settings)
    {
        if (settings.TryGetValue(GovernanceSettingKeys.TenantDelegation.LockSmtp, out var smtp))
            LockSmtp = SettingValueSerializer.Deserialize(smtp.Value, true);
        if (settings.TryGetValue(GovernanceSettingKeys.TenantDelegation.LockStorage, out var storage))
            LockStorage = SettingValueSerializer.Deserialize(storage.Value, true);
        if (settings.TryGetValue(GovernanceSettingKeys.TenantDelegation.LockAnalytics, out var analytics))
            LockAnalytics = SettingValueSerializer.Deserialize(analytics.Value, true);
    }
}
