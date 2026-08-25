// ABOUTME: MediatR query request for fetching a single file type by ID.
// ABOUTME: Returns FileTypeDto.
using Explore.Application.DTOs.FileType;
using MediatR;

namespace Explore.Application.Features.FileTypes.Requests.Queries;

public sealed record GetFileTypeDetailsRequest(int Id = default) : IRequest<FileTypeDto>;
