// ABOUTME: API payload for explicit audited custom-property hard purge requests.
// ABOUTME: Forces operators to provide a durable reason before irreversible deletion.

namespace Explore.Application.DTOs.CustomPropertyDefinition;

public sealed record PurgeCustomPropertyDefinitionDto(string Reason);
