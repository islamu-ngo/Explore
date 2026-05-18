// ABOUTME: Component tests for tenant lookup tables section loading/error/success states.
// ABOUTME: Verifies parallel lookup loading and consolidated lookup tab rendering.

using System.Reflection;
using Explore.Blazor.Client.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Explore.Blazor.Client.Tests.Pages.Admin;

public class LookupTablesTests : IDisposable
{
    private readonly BlazorTestContext _ctx;
    private readonly IAdminService _adminService;
    private readonly IDialogService _dialogService;
    private readonly ISnackbar _snackbar;

    public LookupTablesTests()
    {
        _ctx = new BlazorTestContext();
        _adminService = Substitute.For<IAdminService>();
        _dialogService = Substitute.For<IDialogService>();
        _snackbar = Substitute.For<ISnackbar>();

        _ctx.Services.AddSingleton(_adminService);
        _ctx.Services.AddSingleton(_dialogService);
        _ctx.Services.AddSingleton(_snackbar);

        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Admin User", "admin@example.com");

        SetupDefaultLookups();
    }

    public void Dispose()
    {
        _ctx.Dispose();
    }

    private IRenderedComponent<DynamicComponent> RenderLookupTables()
    {
        var componentType = typeof(IAdminService).Assembly.GetType("Explore.Blazor.Client.Pages.Admin.Tenant.Components.TenantLookupTablesSection")
                            ?? throw new InvalidOperationException("TenantLookupTablesSection component type not found");

        return _ctx.RenderMudComponent<DynamicComponent>(p => p.Add(x => x.Type, componentType));
    }

    [Test]
    public async Task LookupTables_ShowsLoadingIndicator_WhileLookupLoadPending()
    {
        // Arrange
        var pendingEventTypes = new TaskCompletionSource<ICollection<EventTypeListDto>>();
        _adminService.GetEventTypesAsync().Returns(pendingEventTypes.Task);

        // Act
        var cut = RenderLookupTables();

        // Assert
        await Assert.That(cut.Markup).Contains("Lookup Tables");
        await Assert.That(cut.Markup).Contains("Loading lookup tables");

        // Cleanup
        pendingEventTypes.TrySetResult(new List<EventTypeListDto>());
    }

    [Test]
    public async Task LookupTables_ShowsLoadedContent_WhenLookupsSucceed()
    {
        // Arrange
        _adminService.GetEventTypesAsync().Returns(
        [
            new EventTypeListDto
            {
                Id = 1,
                FullName = "Conference",
                Description = "Large gathering"
            }
        ]);

        // Act
        var cut = RenderLookupTables();
        cut.WaitForState(() => cut.Markup.Contains("Lookup Tables", StringComparison.OrdinalIgnoreCase), TimeSpan.FromSeconds(3));

        // Assert
        await Assert.That(cut.Markup).Contains("Event Types");
        await Assert.That(cut.Markup).Contains("Tags");
    }

    [Test]
    public async Task LookupTables_UsesSnackbarError_WhenAnyLookupFails()
    {
        // Arrange
        _adminService.GetEventFormatsAsync().ThrowsAsync(new InvalidOperationException("boom"));

        // Act
        var cut = RenderLookupTables();
        cut.WaitForState(() => cut.Markup.Contains("Failed to load lookup data", StringComparison.OrdinalIgnoreCase), TimeSpan.FromSeconds(3));

        // Assert
        await Assert.That(cut.Markup).Contains("Failed to load lookup data: boom");
    }


    [Test]
    public async Task LookupTables_CreateHelper_UsesSmallDialogOptionsAndReloadsOnSuccess()
    {
        // Arrange
        var component = CreateLookupComponentInstance();
        var createDto = new CreateCategoryDto { FullName = "Community" };
        var dialogReference = Substitute.For<IDialogReference>();
        dialogReference.Result.Returns(DialogResult.Ok(createDto));

        DialogOptions? capturedOptions = null;
        var createCalled = false;
        var reloadCalled = false;

        Func<DialogOptions, Task<IDialogReference>> showDialogAsync = options =>
        {
            capturedOptions = options;
            return Task.FromResult(dialogReference);
        };

        Func<CreateCategoryDto, Task<bool>> createAsync = dto =>
        {
            createCalled = ReferenceEquals(dto, createDto);
            return Task.FromResult(true);
        };

        Func<Task> reloadAsync = () =>
        {
            reloadCalled = true;
            return Task.CompletedTask;
        };

        // Act
        await InvokeLookupCreateAsync(
            component,
            showDialogAsync,
            createAsync,
            dto => dto.FullName,
            "Category",
            reloadAsync);

        // Assert
        await Assert.That(createCalled).IsTrue();
        await Assert.That(reloadCalled).IsTrue();
        await Assert.That(capturedOptions).IsNotNull();
        await Assert.That(capturedOptions!.MaxWidth).IsEqualTo(MaxWidth.Small);
        await Assert.That(capturedOptions.FullWidth).IsTrue();
        await Assert.That(capturedOptions.CloseOnEscapeKey).IsTrue();
        await Assert.That(capturedOptions.CloseButton).IsNull();
        _snackbar.Received(1).Add("Category 'Community' created.", Severity.Success);
    }

    [Test]
    public async Task LookupTables_EditHelper_ShowsFailureWithoutReload_WhenUpdateFails()
    {
        // Arrange
        var component = CreateLookupComponentInstance();
        var updateDto = new UpdateTagDto { FullName = "Youth" };
        var dialogReference = Substitute.For<IDialogReference>();
        dialogReference.Result.Returns(DialogResult.Ok(updateDto));

        var reloadCalled = false;

        // Act
        await InvokeLookupEditAsync<UpdateTagDto>(
            component,
            _ => Task.FromResult(dialogReference),
            _ => Task.FromResult(false),
            dto => dto.FullName,
            "Tag",
            () =>
            {
                reloadCalled = true;
                return Task.CompletedTask;
            });

        // Assert
        await Assert.That(reloadCalled).IsFalse();
        _snackbar.Received(1).Add("Failed to update tag.", Severity.Error);
    }

    [Test]
    public async Task LookupTables_DeleteHelper_DoesNotDelete_WhenConfirmationIsCanceled()
    {
        // Arrange
        var component = CreateLookupComponentInstance();
        var deleteCalled = false;
        var reloadCalled = false;
        _dialogService
            .ShowMessageBoxAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<DialogOptions>())
            .Returns(false);

        // Act
        await InvokeLookupDeleteAsync(
            component,
            Guid.NewGuid(),
            "Community",
            "category",
            _ =>
            {
                deleteCalled = true;
                return Task.FromResult(true);
            },
            () =>
            {
                reloadCalled = true;
                return Task.CompletedTask;
            });

        // Assert
        await Assert.That(deleteCalled).IsFalse();
        await Assert.That(reloadCalled).IsFalse();
    }

    [Test]
    public async Task LookupTables_DeleteHelper_ReloadsAndShowsSuccess_WhenDeleteSucceeds()
    {
        // Arrange
        var component = CreateLookupComponentInstance();
        var locationId = Guid.NewGuid();
        var deletedId = Guid.Empty;
        var reloadCalled = false;
        _dialogService
            .ShowMessageBoxAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<DialogOptions>())
            .Returns(true);

        // Act
        await InvokeLookupDeleteAsync(
            component,
            locationId,
            "Main Hall",
            "location",
            id =>
            {
                deletedId = id;
                return Task.FromResult(true);
            },
            () =>
            {
                reloadCalled = true;
                return Task.CompletedTask;
            });

        // Assert
        await Assert.That(deletedId).IsEqualTo(locationId);
        await Assert.That(reloadCalled).IsTrue();
        _snackbar.Received(1).Add("Location 'Main Hall' deleted.", Severity.Success);
    }

    private void SetupDefaultLookups()
    {
        _adminService.GetEventTypesAsync().Returns(new List<EventTypeListDto>());
        _adminService.GetEventFormatsAsync().Returns(new List<EventFormatListDto>());
        _adminService.GetEventStatusesAsync().Returns(new List<EventStatusListDto>());
        _adminService.GetVisibilityTypesAsync().Returns(new List<VisibilityTypeListDto>());
        _adminService.GetRegistrationModesAsync().Returns(new List<RegistrationModeListDto>());
        _adminService.GetAudienceGendersAsync().Returns(new List<AudienceGenderListDto>());
        _adminService.GetAudienceAgesAsync().Returns(new List<AudienceAgeListDto>());
        _adminService.GetMadhabsAsync().Returns(new List<MadhabListDto>());
        _adminService.GetLanguagesAsync().Returns(new List<LanguageListDto>());
        _adminService.GetOrganizationPositionsAsync().Returns(new List<OrganizationPositionListDto>());
        _adminService.GetApprovalStatusesAsync().Returns(new List<StatusTypeListDto>());
        _adminService.GetActorTypesAsync().Returns(new List<ActorTypeListDto>());
        _adminService.GetFileTypesAsync().Returns(new List<FileTypeListDto>());
        _adminService.GetDidCustodyTypesAsync().Returns(new List<DidCustodyTypeListDto>());
    }


    private object CreateLookupComponentInstance()
    {
        var componentType = typeof(IAdminService).Assembly.GetType("Explore.Blazor.Client.Pages.Admin.Tenant.Components.TenantLookupTablesSection")
                            ?? throw new InvalidOperationException("TenantLookupTablesSection component type not found");
        var component = Activator.CreateInstance(componentType)
                        ?? throw new InvalidOperationException("TenantLookupTablesSection instance could not be created");

        SetNonPublicProperty(componentType, component, "DialogService", _dialogService);
        SetNonPublicProperty(componentType, component, "Snackbar", _snackbar);

        return component;
    }

    private static async Task InvokeLookupCreateAsync<TCreateDto>(
        object component,
        Func<DialogOptions, Task<IDialogReference>> showDialogAsync,
        Func<TCreateDto, Task<bool>> createAsync,
        Func<TCreateDto, string?> getName,
        string entityName,
        Func<Task> reloadAsync)
    {
        await InvokePrivateGenericTask(
            component,
            "RunLookupCreateAsync",
            typeof(TCreateDto),
            showDialogAsync,
            createAsync,
            getName,
            entityName,
            reloadAsync);
    }

    private static async Task InvokeLookupEditAsync<TUpdateDto>(
        object component,
        Func<DialogOptions, Task<IDialogReference>> showDialogAsync,
        Func<TUpdateDto, Task<bool>> updateAsync,
        Func<TUpdateDto, string?> getName,
        string entityName,
        Func<Task> reloadAsync)
    {
        await InvokePrivateGenericTask(
            component,
            "RunLookupEditAsync",
            typeof(TUpdateDto),
            showDialogAsync,
            updateAsync,
            getName,
            entityName,
            reloadAsync);
    }

    private static async Task InvokeLookupDeleteAsync(
        object component,
        Guid? id,
        string? name,
        string entityName,
        Func<Guid, Task<bool>> deleteAsync,
        Func<Task> reloadAsync)
    {
        await InvokePrivateTask(
            component,
            "RunLookupDeleteAsync",
            id,
            name,
            entityName,
            deleteAsync,
            reloadAsync);
    }

    private static async Task InvokePrivateGenericTask(
        object component,
        string methodName,
        Type genericArgument,
        params object?[] args)
    {
        var method = component.GetType()
            .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?.MakeGenericMethod(genericArgument)
            ?? throw new InvalidOperationException($"Method {methodName} not found");

        var task = (Task?)method.Invoke(component, args)
                   ?? throw new InvalidOperationException($"Method {methodName} did not return a task");
        await task;
    }

    private static async Task InvokePrivateTask(object component, string methodName, params object?[] args)
    {
        var method = component.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
                     ?? throw new InvalidOperationException($"Method {methodName} not found");
        var task = (Task?)method.Invoke(component, args)
                   ?? throw new InvalidOperationException($"Method {methodName} did not return a task");
        await task;
    }

    private static void SetNonPublicProperty(Type componentType, object component, string propertyName, object value)
    {
        componentType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(component, value);
    }

}
