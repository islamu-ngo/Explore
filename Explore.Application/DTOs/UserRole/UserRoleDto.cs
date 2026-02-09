using System;
using System.Collections.Generic;

namespace Explore.Application.DTOs.UserRole;

public class UserRoleDto
{
    public int Id { get; set; }
    public required string MasterCode { get; set; }
    public required string FullName { get; set; }
    public string? Description { get; set; }
}
