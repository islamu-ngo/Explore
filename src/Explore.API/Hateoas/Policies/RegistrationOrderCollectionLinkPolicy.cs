// ABOUTME: Defines the intentionally empty registration-order collection HAL policy.
// ABOUTME: Keeps order-state affordances exclusively on detail resources.

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.RegistrationOrders;

namespace Explore.API.Hateoas.Policies;

public sealed class RegistrationOrderCollectionLinkPolicy :
    ICollectionLinkPolicy<RegistrationOrderDto>;
