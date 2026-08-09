// ABOUTME: Handles updates to existing UI themes with scope-aware authorization and deterministic stale-write checks.
// ABOUTME: Preserves a single default theme per scope by clearing competing defaults inside one transaction.

namespace Explore.Application.Features.Appearance.Handlers.Commands;

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Appearance.Validators;
using Explore.Application.Features.Appearance.Common;
using Explore.Application.Features.Appearance.Requests.Commands;
using Explore.Application.Responses;
using MediatR;

public class UpdateUiThemeCommandHandler : IRequestHandler<UpdateUiThemeCommand, BaseCommandResponse<Guid>>
{
    private readonly IUiThemeRepository _uiThemeRepository;
    private readonly IAdminContext _adminContext;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateUiThemeCommandHandler(
        IUiThemeRepository uiThemeRepository,
        IAdminContext adminContext,
        IUnitOfWork unitOfWork)
    {
        _uiThemeRepository = uiThemeRepository;
        _adminContext = adminContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateUiThemeCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();
        var theme = await _uiThemeRepository.GetById(request.Id);

        if (theme is null)
        {
            response.Success = false;
            response.Message = "UI theme not found.";
            response.FailureCode = FailureCodes.NotFound;
            return response;
        }

        if (!await IsAuthorizedAsync(theme, cancellationToken))
        {
            response.Success = false;
            response.Message = theme.TenantId.HasValue
                ? "Only tenant administrators or instance administrators can manage this tenant theme."
                : "Only instance administrators can manage platform themes.";
            response.FailureCode = FailureCodes.AdminRequired;
            return response;
        }

        var validator = new UpdateUiThemeDtoValidator(_uiThemeRepository, theme);
        var validationResult = await validator.ValidateAsync(request.UiThemeDto, cancellationToken);
        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "UI theme update failed.";
            response.Errors = validationResult.Errors.Select(error => error.ErrorMessage).ToList();
            return response;
        }

        if (request.UiThemeDto.RowVersion != theme.RowVersion)
        {
            response.Success = false;
            response.Message = "UI theme update failed because the theme was modified by another administrator.";
            response.Errors = ["The theme has changed since it was loaded. Refresh and try again."];
            return response;
        }

        var willBeDefault = request.UiThemeDto.State?.IsDefault ?? theme.IsDefault;
        if (theme.IsDefault && !willBeDefault)
        {
            response.Success = false;
            response.Message = "A default theme cannot be unset directly.";
            response.Errors = ["Select another default theme before unsetting the current default."];
            return response;
        }

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            if (request.UiThemeDto.State?.IsDefault == true)
            {
                await _uiThemeRepository.ClearDefaultAsync(theme.TenantId, theme.Id);
            }

            UiThemeMapper.Apply(request.UiThemeDto, theme);
            await _uiThemeRepository.Update(theme);
        }, cancellationToken);

        response.Success = true;
        response.Id = theme.Id;
        response.Message = "UI theme updated successfully.";
        return response;
    }

    private async Task<bool> IsAuthorizedAsync(Explore.Domain.UiTheme theme, CancellationToken cancellationToken)
    {
        if (!theme.TenantId.HasValue)
        {
            return await _adminContext.IsInstanceAdminAsync(cancellationToken);
        }

        return await _adminContext.IsTenantAdminAsync(theme.TenantId.Value, cancellationToken)
            || await _adminContext.IsInstanceAdminAsync(cancellationToken);
    }
}
