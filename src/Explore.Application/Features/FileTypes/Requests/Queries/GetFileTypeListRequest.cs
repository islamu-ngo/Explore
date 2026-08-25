// ABOUTME: MediatR query request for fetching all file types.
// ABOUTME: Returns IEnumerable<FileTypeDto>.
using System.Collections.Generic;
using Explore.Application.DTOs.FileType;
using MediatR;

namespace Explore.Application.Features.FileTypes.Requests.Queries;

public sealed record GetFileTypeListRequest : IRequest<List<FileTypeListDto>>
{
}
