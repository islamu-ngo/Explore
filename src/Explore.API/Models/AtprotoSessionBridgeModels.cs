// ABOUTME: Defines the API-host-only request and response for the server-private ATProto session bridge.
// ABOUTME: Stays outside Application DTOs and is excluded from API discovery and generated browser clients.

using System.Text.Json;

namespace Explore.API.Models;

public sealed record BffAtprotoSessionBridgeRequest(
    string ExpectedDid,
    string ExpectedPdsUri,
    string OAuthClientKeyId,
    string Classification,
    JsonElement OAuthSession,
    Guid? CanonicalActorId,
    Guid? ExpectedCanonicalActorConcurrencyStamp);

public sealed record BffAtprotoSessionBridgeResponse(
    Guid UserId,
    Guid ActorId,
    Guid ParticipationId,
    string Did,
    string Classification,
    string AccessToken,
    DateTimeOffset ExpiresAt,
    Guid? CanonicalActorId,
    Guid? ExpectedCanonicalActorConcurrencyStamp);

public sealed record BffAtprotoSessionRefreshResponse(
    Guid UserId,
    string Did,
    string AccessToken,
    DateTimeOffset ExpiresAt);
