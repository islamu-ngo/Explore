namespace Explore.Blazor.Client.Models.DTOs;

public class ProgramRegistrationListDto
{
    public Guid Id { get; set; }
    public Guid ProgramId { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public DateTime RegistrationDate { get; set; }
    public string Status { get; set; } = string.Empty;
}
