namespace Explore.Domain.Enums;

public enum RegistrationModeEnum
{
    Open = 1,           // Anyone can register
    ApprovalRequired = 2,  // Registration requires approval
    InviteOnly = 3,     // Only invited users can register
    Closed = 4          // Registration is closed
}
