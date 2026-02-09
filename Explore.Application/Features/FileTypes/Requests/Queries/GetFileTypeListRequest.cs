using System.Collections.Generic;
using Explore.Application.DTOs.FileType;
using MediatR;

namespace Explore.Application.Features.FileTypes.Requests.Queries;

public class GetFileTypeListRequest : IRequest<List<FileTypeListDto>>
{
}
