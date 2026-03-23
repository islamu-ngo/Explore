// ABOUTME: Handles tenant creation with optional automatic admin assignment for the requesting user.
// ABOUTME: Validates input, enforces slug uniqueness, and atomically creates the tenant + member record.

using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Tenant.Validators;
using Explore.Application.Features.Tenants.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Explore.Application.Features.Tenants.Handlers.Commands.CreateTenant;

public class CreateTenantCommandHandler : IRequestHandler<CreateTenantCommand, BaseCommandResponse<Guid>>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantMemberRepository _tenantMemberRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly ILogger<CreateTenantCommandHandler> _logger;
    private readonly IUnitOfWork _unitOfWork;

    public CreateTenantCommandHandler(
        ITenantRepository tenantRepository,
        ITenantMemberRepository tenantMemberRepository,
        IRoleRepository roleRepository,
        ILogger<CreateTenantCommandHandler> logger,
        IUnitOfWork unitOfWork)
    {
        _tenantRepository = tenantRepository;
        _tenantMemberRepository = tenantMemberRepository;
        _roleRepository = roleRepository;
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();
        var dto = request.TenantDto;

        var validator = new CreateTenantDtoValidator();
        var validation = await validator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
        {
            response.Success = false;
            response.Message = "Invalid tenant data.";
            response.Errors = validation.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        var existingTenant = await _tenantRepository.GetTenantBySlug(dto.Slug);
        if (existingTenant != null)
        {
            response.Success = false;
            response.Message = "A tenant with this slug already exists.";
            response.Errors = ["Slug must be unique across all tenants."];
            return response;
        }

        var statusId = dto.IsActive ? (int)TenantStatusEnum.Active : (int)TenantStatusEnum.Provisioning;
        var assignAdmin = dto.AssignCurrentUserAsTenantAdmin && request.RequestingUserId.HasValue;

        var tenantId = await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var tenant = await _tenantRepository.Create(new Tenant
            {
                FullName = dto.FullName,
                Slug = dto.Slug,
                TenantStatusId = statusId,
                TenantStatus = null!
            });

            if (assignAdmin)
            {
                await AssignRequestingUserAsTenantAdminAsync(tenant.Id, request.RequestingUserId!.Value, ct);
            }

            return tenant.Id;
        }, cancellationToken);

        response.Success = true;
        response.Id = tenantId;
        response.Message = assignAdmin
            ? "Tenant created successfully. You have been assigned as tenant administrator."
            : "Tenant created successfully.";
        return response;
    }

    private async Task AssignRequestingUserAsTenantAdminAsync(Guid tenantId, Guid userId, CancellationToken ct)
    {
        var tenantAdminRole = await _roleRepository.GetByMasterCodeAsync("tenant.admin")
            ?? await _roleRepository.GetByIdAsync((int)RoleEnum.TenantAdmin);

        if (tenantAdminRole == null)
        {
            _logger.LogWarning(
                "tenant.admin role not found; skipping automatic admin assignment for UserId={UserId} on TenantId={TenantId}.",
                userId, tenantId);
            return;
        }

        var existing = await _tenantMemberRepository.GetByTenantAndUser(tenantId, userId);
        if (existing != null) return;

        await _tenantMemberRepository.Create(new TenantMember
        {
            TenantId = tenantId,
            Tenant = null!,
            UserId = userId,
            User = null!,
            RoleId = tenantAdminRole.Id,
            Role = null!,
            GrantedAt = DateTime.UtcNow,
            GrantedBy = userId
        });
    }
}
