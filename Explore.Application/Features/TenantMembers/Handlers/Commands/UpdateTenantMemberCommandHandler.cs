// ABOUTME: Handles tenant member update with validation and mapping.
// ABOUTME: Validates referenced user/role exist before persisting changes.

using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.TenantMember.Validators;
using Explore.Application.Features.TenantMembers.Requests.Commands;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.TenantMembers.Handlers.Commands;

public class UpdateTenantMemberCommandHandler : IRequestHandler<UpdateTenantMemberCommand, BaseCommandResponse<Guid>>
{
    private readonly ITenantMemberRepository _tenantMemberRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IMapper _mapper;

    public UpdateTenantMemberCommandHandler(
        ITenantMemberRepository tenantMemberRepository,
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IMapper mapper)
    {
        _tenantMemberRepository = tenantMemberRepository;
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _mapper = mapper;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateTenantMemberCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var validator = new UpdateTenantMemberDtoValidator(_userRepository, _roleRepository);
        var validationResult = await validator.ValidateAsync(request.TenantMemberDto, cancellationToken);

        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Tenant Member update failed.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        var existingMember = await _tenantMemberRepository.GetById(request.TenantMemberDto.Id);
        if (existingMember == null)
        {
            response.Success = false;
            response.Message = "Tenant Member not found.";
            return response;
        }

        _mapper.Map(request.TenantMemberDto, existingMember);
        await _tenantMemberRepository.Update(existingMember);

        response.Success = true;
        response.Id = existingMember.Id;
        response.Message = "Tenant Member updated successfully.";

        return response;
    }
}
