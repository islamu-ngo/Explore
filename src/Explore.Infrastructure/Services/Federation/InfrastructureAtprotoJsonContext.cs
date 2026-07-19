// ABOUTME: Supplies source-generated JSON metadata for Infrastructure-owned ATProto XRPC responses.
// ABOUTME: Extends CarpaNet client options without reflection fallback or leaking transport models outward.

using System.Text.Json.Serialization;

namespace Explore.Infrastructure.Services.Federation;

internal sealed class InfrastructureAtprotoGetSessionResponse
{
    public string Did { get; set; } = string.Empty;
    public string Handle { get; set; } = string.Empty;
    public bool? Active { get; set; }
}

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(InfrastructureAtprotoGetSessionResponse))]
[JsonSerializable(typeof(AtprotoGetRecordResponse))]
[JsonSerializable(typeof(AtprotoPutRecordInput))]
[JsonSerializable(typeof(AtprotoPutRecordResponse))]
[JsonSerializable(typeof(AtprotoDeleteRecordInput))]
[JsonSerializable(typeof(AtprotoDeleteRecordResponse))]
internal partial class InfrastructureAtprotoJsonContext : JsonSerializerContext;
