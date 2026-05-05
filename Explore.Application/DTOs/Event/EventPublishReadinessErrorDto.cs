namespace Explore.Application.DTOs.Event;

public class EventPublishReadinessErrorDto
{
    public required string Code { get; set; }
    public required string FieldPath { get; set; }
    public required string Message { get; set; }
    public string Severity { get; set; } = "error";
}
