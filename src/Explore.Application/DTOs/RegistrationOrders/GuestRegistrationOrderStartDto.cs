// ABOUTME: Returns a newly created guest order and the opaque capability token exactly once.
// ABOUTME: Keeps the stored hash and all later order representations free of token plaintext.

using Explore.Application.Responses;
using System.Text.Json.Serialization;

namespace Explore.Application.DTOs.RegistrationOrders;

public sealed class GuestRegistrationOrderStartDto : BaseCommandResponse<Guid>
{
    [JsonIgnore]
    public string? GuestCapabilityToken { get; init; }
}
