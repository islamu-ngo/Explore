using Explore.Application.DTOs.AudienceGender;
using MediatR;

namespace Explore.Application.Features.AudienceGenders.Requests.Queries;

public class GetAudienceGenderDetailsRequest : IRequest<AudienceGenderDto>
{
    public int Id { get; set; }
}
