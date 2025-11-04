using System;
using System.Collections.Generic;
using System.Text;
using Explore.Application.DTOs.Program;
using MediatR;

namespace Explore.Application.Features.Programs.Requests.Queries
{
    public class GetProgramListRequest : IRequest<List<ProgramListDto>>
    {
        public Guid Id { get; set; }
        public int ProgramTypeId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int AudienceGenderId { get; set; }
        public int AudienceAgeId { get; set; }
        public Guid OrganizationId { get; set; }
        public int? AudienceAttendees { get; set; }
        public int TotalViews { get; set; }
        public double Price { get; set; }
        public Guid? FeaturedImageId { get; set; }
        public bool? IsRegistrationRequired { get; set; }
        public string? Country { get; set; }
        public string? City { get; set; }
        public int? PostCode { get; set; }
        public string? Address { get; set; }
        public string? ProgramUrl { get; set; }
    }
}
