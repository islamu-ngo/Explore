// ABOUTME: Result model for durable notification intent orchestration.
// ABOUTME: Exposes persisted entities created by the Application orchestrator without transport details.

using Explore.Domain;

namespace Explore.Application.Notifications;

public sealed record NotificationOrchestrationResult(
    NotificationIntent Intent,
    NotificationOwnershipDecision Decision,
    NotificationDelivery? Delivery = null,
    NotificationExternalDelegation? ExternalDelegation = null);
