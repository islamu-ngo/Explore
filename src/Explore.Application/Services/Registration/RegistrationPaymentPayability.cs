// ABOUTME: Defines the single current-payability predicate shared by payment execution and HAL projection.
// ABOUTME: Requires AwaitingPayment, positive due total, and an unexpired current hold boundary.

using Explore.Domain.Enums;

namespace Explore.Application.Services.Registration;

public static class RegistrationPaymentPayability
{
    public static bool IsCurrentlyPayable(int statusId, long totalDueMinor, DateTime? expiresAt, DateTime now) =>
        statusId == (int)RegistrationOrderStatusEnum.AwaitingPayment &&
        totalDueMinor > 0 &&
        expiresAt is { } expiry && expiry > now;
}
