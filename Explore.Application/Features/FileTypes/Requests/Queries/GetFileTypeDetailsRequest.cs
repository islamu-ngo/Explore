using Explore.Application.DTOs.FileType;
using MediatR;

namespace Explore.Application.Features.FileTypes.Requests.Queries
{
    public class GetFileTypeDetailsRequest : IRequest<FileTypeDto>
    {
        public int Id { get; set; }
    }
}
