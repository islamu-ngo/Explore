// ABOUTME: MediatR query request for fetching a single storage object by ID.
// ABOUTME: Returns StorageObjectDto.
using Explore.Application.DTOs.StorageObject;
using MediatR;

namespace Explore.Application.Features.StorageObjects.Requests.Queries;

public class GetStorageObjectDetailsRequest : IRequest<StorageObjectDto?>
{
    public Guid Id { get; set; }
}
