// ABOUTME: Handler for updating an existing tag with validation.
// ABOUTME: Validates input, fetches entity, applies field updates.
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Tag.Validators;
using Explore.Application.Features.Tags.Requests.Commands;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Tags.Handlers.Commands;

public class UpdateTagCommandHandler : IRequestHandler<UpdateTagCommand, BaseCommandResponse<Guid>>
{
    private readonly ITagRepository _tagRepository;
    private readonly ITenantContext _tenantContext;

    public UpdateTagCommandHandler(
        ITagRepository tagRepository,
        ITenantContext tenantContext)
    {
        _tagRepository = tagRepository;
        _tenantContext = tenantContext;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateTagCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var validator = new UpdateTagDtoValidator();
        var validationResult = await validator.ValidateAsync(request.Update, cancellationToken);

        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Tag update failed.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        if (request.TenantId != _tenantContext.TenantId)
        {
            response.Success = false;
            response.Message = "Tag not found.";
            response.FailureCode = FailureCodes.NotFound;
            return response;
        }

        var tag = await _tagRepository.GetById(request.TagId);

        if (tag == null || tag.TenantId != _tenantContext.TenantId)
        {
            response.Success = false;
            response.Message = "Tag not found.";
            response.FailureCode = FailureCodes.NotFound;
            return response;
        }

        if (request.Update.MasterCode is not null)
            tag.MasterCode = request.Update.MasterCode.Value.Trim();

        if (request.Update.FullName is not null)
            tag.FullName = request.Update.FullName.Value.Trim();

        await _tagRepository.Update(tag);

        response.Success = true;
        response.Id = tag.Id;
        response.Message = "Tag updated successfully.";

        return response;
    }
}
