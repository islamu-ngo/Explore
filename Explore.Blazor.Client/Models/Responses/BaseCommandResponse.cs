namespace Explore.Blazor.Client.Models.Responses;

public class BaseCommandResponse<TKey>
{
    public TKey? Id { get; set; }
    public bool Success { get; set; }
    public string? Message { get; set; }
    public List<string>? Errors { get; set; }
}
