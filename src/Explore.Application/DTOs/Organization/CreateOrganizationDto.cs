using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Explore.Domain;

namespace Explore.Application.DTOs.Organization;

public sealed record CreateOrganizationDto
{
    public required string FullName { get; init; }
    public string? WebsiteUrl { get; init; }
    public required string Email { get; init; }
    public required string Country { get; init; }
    public required string City { get; init; }
    public int Postcode { get; init; }
    public required string Address { get; init; }

    /// <summary>
    /// Optional profile picture for the organization's Actor.
    /// This should be the ID of a previously uploaded StorageObject.
    /// </summary>
    public Guid? ProfilePictureId { get; init; }
}
