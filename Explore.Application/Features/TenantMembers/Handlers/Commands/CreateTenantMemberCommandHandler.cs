// ABOUTME: Handles tenant member creation with validation, mapping, and tenant context assignment.
// ABOUTME: Sets TenantId from ITenantContext and populates GrantedAt/GrantedBy audit fields.

using AutoMapper;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.TenantMember.Validators;
using Explore.Application.Features.TenantMembers.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using MediatR;

namespace Explore.Application.Features.TenantMembers.Handlers.Commands;

public class CreateTenantMemberCommandHandler : IRequestHandler<CreateTenantMemberCommand, BaseCommandResponse<Guid>>
{
    private readonly ITenantMemberRepository _tenantMemberRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public CreateTenantMemberCommandHandler(
        ITenantMemberRepository tenantMemberRepository,
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        ITenantContext tenantContext,
        ICurrentUserService currentUserService,
        IMapper mapper)
    {
        _tenantMemberRepository = tenantMemberRepository;
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _tenantContext = tenantContext;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(CreateTenantMemberCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var validator = new CreateTenantMemberDtoValidator(_userRepository, _roleRepository);
        var validationResult = await validator.ValidateAsync(request.TenantMemberDto, cancellationToken);

        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Tenant Member creation failed.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        var tenantMember = _mapper.Map<TenantMember>(request.TenantMemberDto);

        tenantMember.TenantId = _tenantContext.TenantId;
        tenantMember.GrantedAt = DateTime.UtcNow;
        tenantMember.GrantedBy = _currentUserService.UserId;

        tenantMember = await _tenantMemberRepository.Create(tenantMember);

        response.Success = true;
        response.Id = tenantMember.Id;
        response.Message = "Tenant Member created successfully.";

        return response;
    }
}
