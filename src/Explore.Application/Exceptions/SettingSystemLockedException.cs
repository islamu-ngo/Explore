// ABOUTME: Coded application exception for tenant setting mutations blocked by an instance lock.
// ABOUTME: Lets CQRS handlers return the stable setting_system_locked failure contract.

namespace Explore.Application.Exceptions;

public sealed class SettingSystemLockedException(string settingKey)
    : InvalidOperationException($"Setting '{settingKey}' is locked at Instance scope.")
{
    public const string Code = "setting_system_locked";
}
