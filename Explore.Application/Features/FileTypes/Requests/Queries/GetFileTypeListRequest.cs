using Explore.Application.DTOs.FileType;
using MediatR;
using System.Collections.Generic;

namespace Explore.Application.Features.FileTypes.Requests.Queries
{
    public class GetFileTypeListRequest : IRequest<List<FileTypeListDto>>
    {
    }
}
