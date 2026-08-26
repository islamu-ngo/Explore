// ABOUTME: Returns one immutable admission rule outcome with an optional fact and next projection.
// ABOUTME: Keeps deterministic result codes separate from persistence and projection mutation.

using Explore.Domain.Enums;

namespace Explore.Domain;

public sealed class AdmissionCheckInDecision
{
    internal AdmissionCheckInDecision(
        AdmissionCheckInResultCodeEnum resultCode,
        AdmissionCheckInEvent? @event,
        AdmissionCheckInState nextState)
    {
        ResultCode = resultCode;
        Event = @event;
        NextState = nextState;
    }

    public AdmissionCheckInResultCodeEnum ResultCode { get; }
    public AdmissionCheckInEvent? Event { get; }
    public AdmissionCheckInState NextState { get; }
}
