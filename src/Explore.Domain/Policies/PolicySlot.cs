// ABOUTME: Generic wrapper separating a policy value from child scope delegation authority.
// ABOUTME: Each governed field is a PolicySlot — the value and whether child scopes can override it.

namespace Explore.Domain.Policies;

public sealed class PolicySlot<T>
{
    public T? LocalValue { get; set; }
    public ChildOverrideMode OverrideMode { get; set; } = ChildOverrideMode.Allow;

    public PolicySlot() { }

    public PolicySlot(T? localValue, ChildOverrideMode overrideMode = ChildOverrideMode.Allow)
    {
        LocalValue = localValue;
        OverrideMode = overrideMode;
    }
}

public enum ChildOverrideMode
{
    Allow = 0,
    Deny = 1
}
