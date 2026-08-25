namespace Explore.Application.DTOs.Event;

public sealed record EventPublishReadinessErrorDto
{
    public required string Code { get; init; }
    public required string FieldPath { get; init; }
    public required string Message { get; init; }
    public string Severity { get; init; } = "error";
}
