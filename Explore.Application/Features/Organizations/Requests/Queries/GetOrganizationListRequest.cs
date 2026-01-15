using Explore.Domain;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Explore.Application.DTOs.Organization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Organizations.Requests.Queries
{
    public class GetOrganizationListRequest : IRequest<PaginatedResult<OrganizationListDto>>
    {
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the page number (1-based). Defaults to 1.
        /// </summary>
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// Gets or sets the page size. Defaults to 20.
        /// </summary>
        public int PageSize { get; set; } = 20;
    }
}
