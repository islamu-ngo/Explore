// ABOUTME: Red bUnit contract tests for the tenant directory-operator identity administration section.
// ABOUTME: Requires understandable field groups and Save authority derived only from the service's exact HAL result.

using System.Reflection;
using Explore.Blazor.Client.Contracts.Services.Accessibility;
using Explore.Blazor.Client.Pages.Admin.Tenant.Components;
using MudBlazor;

namespace Explore.Blazor.Client.Tests.Pages.Admin;

public sealed class TenantDirectoryOperatorIdentitySectionTests : IDisposable
{
    private const string ComponentTypeName =
        "Explore.Blazor.Client.Pages.Admin.Tenant.Components.TenantDirectoryOperatorIdentitySection";
    private const string ModelTypeName =
        "Explore.Blazor.Client.Services.TenantDirectoryOperatorIdentityAdminModel";

    private readonly BlazorTestContext _ctx = new();

    public void Dispose() => _ctx.Dispose();

    [Test]
    public async Task Render_GroupsLegalContactDisclosureAndCommerceValues_WithDirectionalIslands()
    {
        Type? componentType = ResolveProductionType(ComponentTypeName);
        Type? modelType = ResolveProductionType(ModelTypeName);
        await Assert.That(componentType).IsNotNull();
        await Assert.That(modelType).IsNotNull();
        if (componentType is null || modelType is null) return;

        object model = CreateModel(modelType, canEdit: true);
        IRenderedComponent<DynamicComponent> cut = Render(componentType, model);

        await Assert.That(cut.Markup).Contains("Legal identity");
        await Assert.That(cut.Markup).Contains("Contact and support");
        await Assert.That(cut.Markup).Contains("Legal and disclosure links");
        await Assert.That(cut.Markup).Contains("Commerce readiness");
        await Assert.That(Fields(cut, "Public name").Count).IsEqualTo(1);
        await Assert.That(Fields(cut, "Legal name").Count).IsEqualTo(1);
        await Assert.That(Fields(cut, "Public contact email").Count).IsEqualTo(1);
        await Assert.That(Fields(cut, "Legal notice URL").Count).IsEqualTo(1);
        await Assert.That(Fields(cut, "Terms URL").Count).IsEqualTo(1);
        await Assert.That(Fields(cut, "Privacy URL").Count).IsEqualTo(1);
        await Assert.That(cut.FindAll("[dir='ltr']").Count).IsGreaterThanOrEqualTo(6);
        await Assert.That(cut.FindAll("[role='status'][aria-live='polite']").Count).IsEqualTo(1);
        await Assert.That(cut.FindAll("[role='alert']").Count).IsEqualTo(1);
    }

    [Test]
    public async Task Save_IsRenderedAndEnabledOnlyWhenExactHalEditAffordanceWasMapped()
    {
        Type? componentType = ResolveProductionType(ComponentTypeName);
        Type? modelType = ResolveProductionType(ModelTypeName);
        await Assert.That(componentType).IsNotNull();
        await Assert.That(modelType).IsNotNull();
        if (componentType is null || modelType is null) return;

        using var readOnlyContext = new BlazorTestContext();
        using var editableContext = new BlazorTestContext();

        IRenderedComponent<DynamicComponent> readOnly =
            Render(readOnlyContext, componentType, CreateModel(modelType, canEdit: false));
        await Assert.That(SaveButtons(readOnly).Count).IsEqualTo(0);
        await Assert.That(readOnly.FindAll("input[readonly]").Count).IsGreaterThanOrEqualTo(8);
        await Assert.That(readOnly.FindAll("input[disabled]").Count).IsEqualTo(0);
        await Assert.That(readOnly.FindAll("input[readonly]").All(input => input.GetAttribute("tabindex") != "-1")).IsTrue();

        IRenderedComponent<DynamicComponent> editable =
            Render(editableContext, componentType, CreateModel(modelType, canEdit: true));
        IReadOnlyList<AngleSharp.Dom.IElement> saveButtons = SaveButtons(editable);
        await Assert.That(saveButtons.Count).IsEqualTo(1);
        await Assert.That(saveButtons[0].HasAttribute("disabled")).IsFalse();
    }

    [Test]
    public async Task InvalidSubmit_ShowsAlertMarksFieldsAndFocusesFirstInvalid()
    {
        Type componentType = ResolveProductionType(ComponentTypeName)!;
        Type modelType = ResolveProductionType(ModelTypeName)!;
        object model = CreateModel(modelType, canEdit: true);
        Set(model, "PublicName", string.Empty);
        IRenderedComponent<DynamicComponent> cut = Render(componentType, model);
        IAccessibilityFocusService focus = _ctx.Services.GetRequiredService<IAccessibilityFocusService>();

        await cut.Find("[data-testid='save-tenant-directory-operator-identity']").ClickAsync(new());

        await Assert.That(cut.FindAll("[data-testid='operator-validation-summary'][role='alert']")).HasSingleItem();
        await Assert.That(cut.Find("#operator-public-name").GetAttribute("aria-invalid")).IsEqualTo("true");
        await focus.Received(1).FocusAsync("#operator-public-name");
    }

    [Test]
    public async Task Conflict_PreservesPendingEditsShowsAuthoritativeValuesAndFocusesAlert()
    {
        var pending = (TenantDirectoryOperatorIdentityAdminModel)CreateModel(typeof(TenantDirectoryOperatorIdentityAdminModel), canEdit: true);
        pending.PublicName = "My pending edit";
        var authoritative = (TenantDirectoryOperatorIdentityAdminModel)CreateModel(typeof(TenantDirectoryOperatorIdentityAdminModel), canEdit: true);
        authoritative.PublicName = "Authoritative value";
        authoritative.ConcurrencyStamp = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        using var context = new BlazorTestContext();
        context.Services.AddSingleton<ITenantDirectoryOperatorIdentityAdminService>(new ConflictService(authoritative));
        IAccessibilityFocusService focus = context.Services.GetRequiredService<IAccessibilityFocusService>();
        var cut = context.RenderMudComponent<TenantDirectoryOperatorIdentitySection>(p => p.Add(c => c.Model, pending));

        await cut.Find("[data-testid='save-tenant-directory-operator-identity']").ClickAsync(new());

        await Assert.That(pending.PublicName).IsEqualTo("My pending edit");
        await Assert.That(pending.ConcurrencyStamp).IsEqualTo(authoritative.ConcurrencyStamp);
        await Assert.That(cut.Find("[data-testid='operator-concurrency-conflict']").TextContent).Contains("Authoritative value");
        await focus.Received(1).FocusAsync("#operator-conflict-alert");
    }

    [Test]
    public async Task ConflictReloadFailure_PreservesPendingRevisionAndOmitsBlankAuthorityPanel()
    {
        var pending = (TenantDirectoryOperatorIdentityAdminModel)CreateModel(
            typeof(TenantDirectoryOperatorIdentityAdminModel),
            canEdit: true);
        Guid pendingRevision = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        pending.ConcurrencyStamp = pendingRevision;
        pending.PublicName = "My pending edit";
        using var context = new BlazorTestContext();
        context.Services.AddSingleton<ITenantDirectoryOperatorIdentityAdminService>(
            new ConflictService(TenantDirectoryOperatorIdentityAdminModel.Failed()));
        var cut = context.RenderMudComponent<TenantDirectoryOperatorIdentitySection>(
            parameters => parameters.Add(component => component.Model, pending));

        await cut.Find("[data-testid='save-tenant-directory-operator-identity']").ClickAsync(new());

        await Assert.That(pending.PublicName).IsEqualTo("My pending edit");
        await Assert.That(pending.ConcurrencyStamp).IsEqualTo(pendingRevision);
        await Assert.That(cut.FindAll("[data-testid='operator-concurrency-conflict']")).IsEmpty();
    }

    [Test]
    public async Task FirstInvalidFocusOrder_IsExplicitRatherThanDictionaryOrder()
    {
        var model = (TenantDirectoryOperatorIdentityAdminModel)CreateModel(
            typeof(TenantDirectoryOperatorIdentityAdminModel),
            canEdit: true);
        _ctx.Services.AddSingleton(Substitute.For<ITenantDirectoryOperatorIdentityAdminService>());
        var cut = _ctx.RenderMudComponent<TenantDirectoryOperatorIdentitySection>(
            parameters => parameters.Add(component => component.Model, model));
        FieldInfo errorsField = typeof(TenantDirectoryOperatorIdentitySection)
            .GetField("_validationErrors", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var errors = (Dictionary<string, string>)errorsField.GetValue(cut.Instance)!;
        errors[nameof(model.LegalName)] = "legal";
        errors[nameof(model.PublicName)] = "public";
        MethodInfo firstInvalid = typeof(TenantDirectoryOperatorIdentitySection)
            .GetMethod("FirstInvalidId", BindingFlags.Instance | BindingFlags.NonPublic)!;

        string result = (string)firstInvalid.Invoke(cut.Instance, null)!;

        await Assert.That(result).IsEqualTo("operator-public-name");
    }

    [Test]
    public async Task EveryBoundedMessageCode_MapsToCopy_AndUnknownCodesFailLoudly()
    {
        var model = (TenantDirectoryOperatorIdentityAdminModel)CreateModel(
            typeof(TenantDirectoryOperatorIdentityAdminModel),
            canEdit: true);
        _ctx.Services.AddSingleton(Substitute.For<ITenantDirectoryOperatorIdentityAdminService>());
        var cut = _ctx.RenderMudComponent<TenantDirectoryOperatorIdentitySection>(
            parameters => parameters.Add(component => component.Model, model));
        MethodInfo messageFor = typeof(TenantDirectoryOperatorIdentitySection)
            .GetMethod("MessageFor", BindingFlags.Instance | BindingFlags.NonPublic)!;

        foreach (TenantDirectoryOperatorIdentityAdminMessageCode code in
                 Enum.GetValues<TenantDirectoryOperatorIdentityAdminMessageCode>()
                     .Where(code => code != TenantDirectoryOperatorIdentityAdminMessageCode.None))
        {
            string mapped = (string)messageFor.Invoke(cut.Instance, [code])!;
            await Assert.That(mapped).IsNotEmpty();
            await Assert.That(mapped).IsNotEqualTo(code.ToString());
        }

        Exception? observed = null;
        try
        {
            _ = messageFor.Invoke(
                cut.Instance,
                [(TenantDirectoryOperatorIdentityAdminMessageCode)int.MaxValue]);
        }
        catch (TargetInvocationException exception)
        {
            observed = exception.InnerException;
        }

        await Assert.That(observed).IsTypeOf<ArgumentOutOfRangeException>();
    }

    private sealed class ConflictService(TenantDirectoryOperatorIdentityAdminModel authoritative)
        : ITenantDirectoryOperatorIdentityAdminService
    {
        public Task<TenantDirectoryOperatorIdentityAdminModel> GetAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(authoritative);
        public Task<TenantDirectoryOperatorIdentitySaveResult> SaveAsync(TenantDirectoryOperatorIdentityAdminModel model, CancellationToken cancellationToken = default) =>
            Task.FromResult(TenantDirectoryOperatorIdentitySaveResult.Conflict(authoritative));
    }

    private IRenderedComponent<DynamicComponent> Render(Type componentType, object model)
    {
        return Render(_ctx, componentType, model);
    }

    private static IRenderedComponent<DynamicComponent> Render(
        BlazorTestContext context,
        Type componentType,
        object model)
    {
        RegisterNoOpServiceIfRequired(context, componentType);
        return context.RenderMudComponent<DynamicComponent>(parameters => parameters
            .Add(component => component.Type, componentType)
            .Add(component => component.Parameters, new Dictionary<string, object>
            {
                ["Model"] = model
            }));
    }

    private static void RegisterNoOpServiceIfRequired(
        BlazorTestContext context,
        Type componentType)
    {
        foreach (PropertyInfo property in componentType.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                     .Where(property => property.GetCustomAttribute<InjectAttribute>() is not null)
                     .Where(property => property.PropertyType.Name.Contains("DirectoryOperatorIdentity", StringComparison.Ordinal)))
        {
            object proxy = DispatchProxy.Create(property.PropertyType, typeof(NoOpServiceProxy));
            context.Services.AddSingleton(property.PropertyType, proxy);
        }
    }

    private static object CreateModel(Type modelType, bool canEdit)
    {
        object model = Activator.CreateInstance(modelType)
            ?? throw new InvalidOperationException($"Could not create {modelType.Name}.");
        Set(model, "Exists", true);
        Set(model, "CanEdit", canEdit);
        Set(model, "ConcurrencyStamp", Guid.Parse("66666666-6666-6666-6666-666666666666"));
        Set(model, "PublicName", "Community Directory");
        Set(model, "LegalName", "Community Directory Foundation");
        Set(model, "OperatorKindCode", "NONPROFIT");
        Set(model, "JurisdictionCountryCode", "DE");
        Set(model, "RegistrationIdentifier", "VR 12345");
        Set(model, "PublicContactEmail", "support@directory.example");
        Set(model, "LegalNoticeUrl", "https://directory.example/legal");
        Set(model, "TermsUrl", "https://directory.example/terms");
        Set(model, "PrivacyUrl", "https://directory.example/privacy");
        Set(model, "IsActivationReady", true);
        Set(model, "IsPublicDisclosureReady", true);
        Set(model, "IsPaidCommerceReady", true);
        return model;
    }

    private static void Set(object target, string propertyName, object value)
    {
        PropertyInfo property = target.GetType().GetProperty(propertyName)
            ?? throw new InvalidOperationException($"{target.GetType().Name} does not expose {propertyName}.");
        property.SetValue(target, value);
    }

    private static IReadOnlyList<IRenderedComponent<MudTextField<string>>> Fields(
        IRenderedComponent<DynamicComponent> cut,
        string label) => cut.FindComponents<MudTextField<string>>()
            .Where(field => field.Instance.Label == label)
            .ToArray();

    private static IReadOnlyList<AngleSharp.Dom.IElement> SaveButtons(IRenderedComponent<DynamicComponent> cut) =>
        cut.FindAll("button")
            .Where(button => button.TextContent.Trim().Equals("Save", StringComparison.OrdinalIgnoreCase))
            .ToArray();

    private static Type? ResolveProductionType(string fullName) =>
        typeof(TenantBrandingSettingsAdminService).Assembly.GetType(fullName, throwOnError: false);

    private class NoOpServiceProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.ReturnType == typeof(Task)) return Task.CompletedTask;
            if (targetMethod?.ReturnType.IsGenericType == true
                && targetMethod.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                Type resultType = targetMethod.ReturnType.GetGenericArguments()[0];
                object? result = resultType.IsValueType ? Activator.CreateInstance(resultType) : null;
                return typeof(Task).GetMethod(nameof(Task.FromResult))!
                    .MakeGenericMethod(resultType)
                    .Invoke(null, [result]);
            }

            return targetMethod?.ReturnType.IsValueType == true
                ? Activator.CreateInstance(targetMethod.ReturnType)
                : null;
        }
    }
}
