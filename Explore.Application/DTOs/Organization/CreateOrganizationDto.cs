using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Explore.Domain;

namespace Explore.Application.DTOs.Organization;

public class CreateOrganizationDto
{
    public required string FullName { get; set; }
    public string? WebsiteUrl { get; set; }
    public required string Email { get; set; }
    public required string Country { get; set; }
    public required string City { get; set; }
    public int Postcode { get; set; }
    public required string Address { get; set; }

    /// <summary>
    /// Optional profile picture for the organization's Actor.
    /// This should be the ID of a previously uploaded StorageObject.
    /// </summary>
    public Guid? ProfilePictureId { get; set; }
}
