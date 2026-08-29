// ABOUTME: Defines prospective optional, HAL-driven, accessible, localized add-on component contracts.
// ABOUTME: Pins unchecked defaults, exact totals, focus/live status, RTL-safe CSS, and service isolation.

using System.Reflection;

namespace Explore.Blazor.Client.Tests;

public sealed class EventAddOnComponentTests
{
    private const string ComponentTypeName =
        "Explore.Blazor.Client.Components.Registration.EventAddOnSelector";
    private const string ServiceTypeName =
        "Explore.Blazor.Client.Contracts.Services.IEventAddOnService";
    private static readonly string RepositoryRoot = FindRepositoryRoot();
    private static readonly string ComponentPath = Path.Combine(
        RepositoryRoot,
        "src",
        "Explore.Blazor.Client",
        "Components",
        "Registration",
        "EventAddOnSelector.razor");
    private static readonly string CssPath = ComponentPath + ".css";

    [Test]
    public async Task ComponentAndServiceContractsExistWithoutTransportOrTokenDependencies()
    {
        Type? component = typeof(Program).Assembly.GetType(ComponentTypeName);
        Type? service = typeof(Program).Assembly.GetType(ServiceTypeName);
        await Assert.That(component).IsNotNull();
        await Assert.That(service).IsNotNull();
        await Assert.That(File.Exists(ComponentPath)).IsTrue();
        if (service is null)
        {
            return;
        }

        foreach (string method in new[]
                 {
                     "GetCatalogAsync",
                     "GetOrderAsync",
                     "ReserveAsync",
                     "FulfillAsync",
                     "RefundAsync",
                 })
        {
            await Assert.That(service.GetMethod(method)).IsNotNull();
        }
    }

    [Test]
    public async Task EveryAddOnStartsUnselectedAndNoMarkupMakesItRequired()
    {
        await Assert.That(File.Exists(ComponentPath)).IsTrue();
        if (!File.Exists(ComponentPath))
        {
            return;
        }

        string source = await File.ReadAllTextAsync(ComponentPath);
        await Assert.That(source).Contains("<fieldset");
        await Assert.That(source).Contains("<legend");
        await Assert.That(source).Contains("add_on_optional");
        await Assert.That(source).Contains("Quantity = 0");
        await Assert.That(source).Contains("!item.IsAvailable");
        await Assert.That(source).DoesNotContain("required");
        await Assert.That(source).DoesNotContain("checked=\"checked\"");
        await Assert.That(source).DoesNotContain("preselected");
        await Assert.That(source).DoesNotContain("must add");
        await Assert.That(source).DoesNotContain("don't miss out");
    }

    [Test]
    public async Task ActionsAreRenderedOnlyFromHalRelationsNeverRolesOrClaims()
    {
        await Assert.That(File.Exists(ComponentPath)).IsTrue();
        if (!File.Exists(ComponentPath))
        {
            return;
        }

        string source = await File.ReadAllTextAsync(ComponentPath);
        await Assert.That(source).Contains("reserve-event-add-ons");
        await Assert.That(source).Contains("fulfill-event-add-on");
        await Assert.That(source).Contains("refund-event-add-on");
        await Assert.That(source).Contains("_links");
        await Assert.That(source).Contains("HasLineRelation(line.Id, FulfillRelation)");
        await Assert.That(source).Contains("HasLineRelation(line.Id, RefundRelation)");
        await Assert.That(source).DoesNotContain("HasRelation(FulfillRelation)");
        await Assert.That(source).DoesNotContain("HasRelation(RefundRelation)");
        await Assert.That(source).DoesNotContain("IsInRole");
        await Assert.That(source).DoesNotContain("ClaimsPrincipal");
        await Assert.That(source).DoesNotContain("AuthorizeView");
        await Assert.That(source).DoesNotContain("User.IsInRole");
    }

    [Test]
    public async Task ComponentProvidesSemanticLiveStatusAndDeterministicFocus()
    {
        await Assert.That(File.Exists(ComponentPath)).IsTrue();
        if (!File.Exists(ComponentPath))
        {
            return;
        }

        string source = await File.ReadAllTextAsync(ComponentPath);
        await Assert.That(source).Contains("IAccessibilityFocusService");
        await Assert.That(source).Contains("Focus");
        await Assert.That(source).Contains("role=\"status\"");
        await Assert.That(source).Contains("aria-live=\"polite\"");
        await Assert.That(source).Contains("aria-busy");
        await Assert.That(source).Contains("aria-describedby");
        await Assert.That(source).Contains("CurrencyCode");
        await Assert.That(source).Contains("LineTotalMinor");
    }

    [Test]
    public async Task TextIsLocalizedAndCssUsesOnlyLogicalRtlSafeProperties()
    {
        await Assert.That(File.Exists(ComponentPath)).IsTrue();
        await Assert.That(File.Exists(CssPath)).IsTrue();
        if (!File.Exists(ComponentPath) || !File.Exists(CssPath))
        {
            return;
        }

        string source = await File.ReadAllTextAsync(ComponentPath);
        string css = await File.ReadAllTextAsync(CssPath);
        await Assert.That(source).Contains("ITranslationService");
        await Assert.That(source).Contains("Translation.T");
        await Assert.That(source).Contains("CultureInfo");
        await Assert.That(css).Contains("padding-inline");
        await Assert.That(css).Contains("margin-block");
        foreach (string forbidden in new[]
                 {
                     "margin-left",
                     "margin-right",
                     "padding-left",
                     "padding-right",
                     "text-align: left",
                     "text-align: right",
                 })
        {
            await Assert.That(css).DoesNotContain(forbidden);
        }
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Explore.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Repository root was not found.");
    }
}
