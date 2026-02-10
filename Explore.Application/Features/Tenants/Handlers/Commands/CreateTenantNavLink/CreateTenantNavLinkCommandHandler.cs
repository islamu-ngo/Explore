using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.Tenants.Requests.Commands.CreateTenantNavLink;
using Explore.Application.Responses;
using Explore.Domain;
using MediatR;

namespace Explore.Application.Features.Tenants.Handlers.Commands.CreateTenantNavLink;

/// <summary>
/// Handler for CreateTenantNavLinkCommand.
/// Creates a new navigation link for the current tenant.
/// Automatically assigns the next order value.
/// </summary>
public class CreateTenantNavLinkCommandHandler : IRequestHandler<CreateTenantNavLinkCommand, BaseCommandResponse<Guid>>
{
    private readonly ITenantNavigationLinkRepository _navigationLinkRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IMapper _mapper;

    public CreateTenantNavLinkCommandHandler(
        ITenantNavigationLinkRepository navigationLinkRepository,
        ITenantContext tenantContext,
        IMapper mapper)
    {
        _navigationLinkRepository = navigationLinkRepository;
        _tenantContext = tenantContext;
        _mapper = mapper;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(CreateTenantNavLinkCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        // Map DTO to entity
        var navigationLink = _mapper.Map<TenantNavigationLink>(request.NavigationLinkDto);

        // Set tenant ID from context
        navigationLink.TenantId = _tenantContext.TenantId;

        // Get the next order value
        var maxOrder = await _navigationLinkRepository.GetMaxOrderByTenantIdAsync(
            _tenantContext.TenantId,
            cancellationToken);
        navigationLink.Order = maxOrder + 1;

        // Set default active state
        navigationLink.IsActive = true;

        // Create the navigation link
        navigationLink = await _navigationLinkRepository.Create(navigationLink);

        response.Success = true;
        response.Id = navigationLink.Id;
        response.Message = "Navigation link created successfully.";

        return response;
    }
}
