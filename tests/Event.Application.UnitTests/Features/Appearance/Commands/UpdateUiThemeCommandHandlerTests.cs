// ABOUTME: Unit tests for grouped UI theme PATCH handling and route-owned identity.
// ABOUTME: Verifies omitted groups remain unchanged and merged default-state validation is atomic.

namespace Event.Application.UnitTests.Features.Appearance.Commands;

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Appearance;
using Explore.Application.Features.Appearance.Handlers.Commands;
using Explore.Application.Features.Appearance.Requests.Commands;
using Explore.Application.Models.Common;
using Explore.Domain;
using Explore.Domain.ValueObjects;
using NSubstitute;

public class UpdateUiThemeCommandHandlerTests
{
    private readonly IUiThemeRepository _repository = Substitute.For<IUiThemeRepository>();
    private readonly IAdminContext _adminContext = Substitute.For<IAdminContext>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly UpdateUiThemeCommandHandler _handler;

    public UpdateUiThemeCommandHandlerTests()
    {
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(true);
        _repository.ThemeKeyExistsAsync(Arg.Any<Guid?>(), Arg.Any<string>(), Arg.Any<Guid?>()).Returns(false);
        _unitOfWork
            .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task>>()(CancellationToken.None));
        _handler = new UpdateUiThemeCommandHandler(_repository, _adminContext, _unitOfWork);
    }

    [Test]
    public async Task Handle_WithMetadataOnly_PreservesStateAndPalettes()
    {
        var theme = CreateTheme();
        theme.IsDefault = true;
        var originalLight = theme.LightPalette;
        var originalDark = theme.DarkPalette;
        _repository.GetById(theme.Id).Returns(theme);

        var result = await _handler.Handle(new UpdateUiThemeCommand
        {
            Id = theme.Id,
            UiThemeDto = new UpdateUiThemeDto
            {
                RowVersion = theme.RowVersion,
                Metadata = new UpdateUiThemeMetadataDto
                {
                    DisplayName = " Updated ",
                    Description = OptionalUpdate<string>.Set(null)
                }
            }
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(theme.DisplayName).IsEqualTo("Updated");
        await Assert.That(theme.Description).IsNull();
        await Assert.That(theme.IsActive).IsTrue();
        await Assert.That(theme.IsDefault).IsTrue();
        await Assert.That(theme.LightPalette).IsSameReferenceAs(originalLight);
        await Assert.That(theme.DarkPalette).IsSameReferenceAs(originalDark);
        await _repository.DidNotReceive().ClearDefaultAsync(Arg.Any<Guid?>(), Arg.Any<Guid?>());
        await _repository.Received(1).Update(theme);
    }

    [Test]
    public async Task Handle_WithEmptyWrapper_RejectsWithoutTransaction()
    {
        var theme = CreateTheme();
        _repository.GetById(theme.Id).Returns(theme);

        var result = await _handler.Handle(new UpdateUiThemeCommand
        {
            Id = theme.Id,
            UiThemeDto = new UpdateUiThemeDto { RowVersion = theme.RowVersion }
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await _unitOfWork.DidNotReceive().ExecuteInTransactionAsync(
            Arg.Any<Func<CancellationToken, Task>>(),
            Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().Update(Arg.Any<UiTheme>());
    }

    [Test]
    public async Task Handle_WhenDefaultThemeWouldBecomeInactive_RejectsMergedState()
    {
        var theme = CreateTheme();
        theme.IsDefault = true;
        _repository.GetById(theme.Id).Returns(theme);

        var result = await _handler.Handle(new UpdateUiThemeCommand
        {
            Id = theme.Id,
            UiThemeDto = new UpdateUiThemeDto
            {
                RowVersion = theme.RowVersion,
                State = new UpdateUiThemeStateDto { IsActive = false }
            }
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await _repository.DidNotReceive().Update(Arg.Any<UiTheme>());
    }

    private static UiTheme CreateTheme() => new()
    {
        Id = Guid.NewGuid(),
        ThemeKey = "platform-theme",
        DisplayName = "Platform Theme",
        Description = "Description",
        IsActive = true,
        IsDefault = false,
        SortOrder = 10,
        RowVersion = 5,
        LightPalette = CreatePalette("#336699"),
        DarkPalette = CreatePalette("#6699CC")
    };

    private static UiThemePalette CreatePalette(string primary) => new()
    {
        Primary = primary,
        PrimaryContrastText = "#FFFFFF",
        Secondary = "#112233",
        SecondaryContrastText = "#FFFFFF",
        Background = "#F8FAFC",
        Surface = "#FFFFFF",
        AppbarBackground = "rgba(51,102,153,0.85)",
        AppbarText = "#FFFFFF",
        DrawerBackground = "#0F172A",
        DrawerText = "#E2E8F0",
        DrawerIcon = "#CBD5E1",
        TextPrimary = "#0F172A",
        TextSecondary = "#64748B",
        Info = "#3B82F6",
        Success = "#10B981",
        Warning = "#F59E0B",
        Error = "#EF4444",
        LinesDefault = "#E2E8F0",
        Divider = "rgba(51,102,153,0.25)"
    };
}
