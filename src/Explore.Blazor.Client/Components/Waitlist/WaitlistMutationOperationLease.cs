// ABOUTME: Retains one UUIDv7 idempotency identity across ambiguous waitlist mutation retries.
// ABOUTME: Rotates identity only for a different action target or after a definitive response.

namespace Explore.Blazor.Client.Components.Waitlist;

internal sealed class WaitlistMutationOperationLease
{
    private string? _key;
    private Guid _operationId;

    public Guid Acquire(string key)
    {
        if (string.Equals(
                _key,
                key,
                StringComparison.Ordinal))
        {
            return _operationId;
        }

        _key = key;
        _operationId = Guid.CreateVersion7();
        return _operationId;
    }

    public void Complete(string key)
    {
        if (!string.Equals(
                _key,
                key,
                StringComparison.Ordinal))
        {
            return;
        }

        _key = null;
        _operationId = Guid.Empty;
    }
}
