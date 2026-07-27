// ABOUTME: Applies grouped Application-only updates to tag-to-tag-type junctions.
// ABOUTME: Enforces persisted tenant ownership and duplicate-pair rejection before one save.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.TagTypeTags.Validators;
using Explore.Application.Features.TagTypeTags.Requests.Commands;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.TagTypeTags.Handlers.Commands;

public class UpdateTagTypeTagsCommandHandler : IRequestHandler<UpdateTagTypeTagsCommand, BaseCommandResponse<Guid>>
{
    private readonly ITagTypeTagsRepository _repository;
    private readonly ITagRepository _tagRepository;
    private readonly ITagTypeRepository _tagTypeRepository;
    private readonly ITenantContext _tenantContext;

    public UpdateTagTypeTagsCommandHandler(
        ITagTypeTagsRepository repository,
        ITagRepository tagRepository,
        ITagTypeRepository tagTypeRepository,
        ITenantContext tenantContext)
    {
        _repository = repository;
        _tagRepository = tagRepository;
        _tagTypeRepository = tagTypeRepository;
        _tenantContext = tenantContext;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateTagTypeTagsCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var validator = new UpdateTagTypeTagsDtoValidator();
        var validationResult = await validator.ValidateAsync(request.TagTypeTagsDto, cancellationToken);

        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Tag Type Tags update failed.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        var link = await _repository.GetById(request.TagTypeTagsId);
        if (link == null || link.TenantId != _tenantContext.TenantId)
        {
            response.Success = false;
            response.Message = "Tag Type Tags not found.";
            return response;
        }

        Guid tagId = request.TagTypeTagsDto.Relationship?.TagId ?? link.TagId;
        int tagTypeId = request.TagTypeTagsDto.Relationship?.TagTypeId ?? link.TagTypeId;
        var tag = await _tagRepository.GetById(tagId);
        if (tag is null || tag.TenantId != link.TenantId || !await _tagTypeRepository.Exists(tagTypeId))
        {
            response.Success = false;
            response.Message = "Tag Type Tags update failed.";
            response.Errors = ["Relationship targets were not found in the current tenant."];
            return response;
        }

        if ((tagId != link.TagId || tagTypeId != link.TagTypeId) && await _repository.Exists(tagId, tagTypeId))
        {
            response.Success = false;
            response.Message = "Tag Type Tags update failed.";
            response.Errors = ["Tag and Tag Type relationship already exists."];
            return response;
        }

        link.TagId = tagId;
        link.TagTypeId = tagTypeId;
        await _repository.Update(link);

        response.Success = true;
        response.Id = link.Id;
        response.Message = "Tag Type Tags updated successfully.";

        return response;
    }
}
