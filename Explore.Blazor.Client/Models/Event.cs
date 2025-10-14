namespace Explore.Blazor.Client.Models;

public class Event
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Country { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty; // Keep for backward compatibility
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string LocationCountry { get; set; } = string.Empty;
    public string? Url { get; set; } = string.Empty;
    public bool IsOnline { get; set; } = false;
}
