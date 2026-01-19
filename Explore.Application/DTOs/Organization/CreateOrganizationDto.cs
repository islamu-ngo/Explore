using Explore.Domain;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Explore.Application.DTOs.Organization
{
    public class CreateOrganizationDto
    {
        public string FullName { get; set; }
        public string? WebsiteUrl { get; set; }
        public string Email { get; set; }
        public string Country { get; set; }
        public string City { get; set; }
        public int Postcode { get; set; }
        public string Address { get; set; }

        /// <summary>
        /// Optional profile picture for the organization's Actor.
        /// This should be the ID of a previously uploaded StorageObject.
        /// </summary>
        public Guid? ProfilePictureId { get; set; }
    }
}
