namespace Explore.Blazor.Client.Models.Enums;

/// <summary>
/// Organization role enum - mirrors the backend OrganizationRoleEnum
/// </summary>
public enum OrganizationRole
{
    Creator = 1,
    CoOwner = 2,
    Admin = 3,
    Moderator = 4,
    Member = 5,
    Viewer = 6
}
