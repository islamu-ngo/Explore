// ABOUTME: Component tests for the authenticated event-report submission dialog.
// ABOUTME: Verifies client validation mirrors the report-intake command contract before API submission.

using System.Reflection;
using Explore.Blazor.Client.Components.EventReporting;
using Explore.Blazor.Client.Contracts.Services.EventReporting;

namespace Explore.Blazor.Client.Tests.Components.EventReporting;

public sealed class ReportEventDialogTests : IDisposable
{
    private readonly BlazorTestContext _ctx = new();

    [Test]
    public async Task Render_WhenDetailsEmpty_DisablesSubmitUntilDetailsAreProvided()
    {
        var eventId = Guid.NewGuid();
        var service = RegisterReportingService(eventId);

        var cut = RenderDialog(eventId);
        cut.WaitForState(() => !GetPrivateField<bool>(cut.Instance, "_isLoadingOptions"), TimeSpan.FromSeconds(3));

        await Assert.That(GetPrivateProperty<bool>(cut.Instance, "CanSubmit")).IsFalse();

        SetPrivateField(cut.Instance, "_reporterText", "This event appears to be spam.");

        await Assert.That(GetPrivateProperty<bool>(cut.Instance, "CanSubmit")).IsTrue();
        await service.DidNotReceive().SubmitAsync(Arg.Any<SubmitEventReportDto>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SubmitAsync_WhenDetailsEmpty_DoesNotCallSubmissionService()
    {
        var eventId = Guid.NewGuid();
        var service = RegisterReportingService(eventId);

        var cut = RenderDialog(eventId);
        cut.WaitForState(() => !GetPrivateField<bool>(cut.Instance, "_isLoadingOptions"), TimeSpan.FromSeconds(3));

        await InvokePrivateTaskAsync(cut.Instance, "SubmitAsync");

        await service.DidNotReceive().SubmitAsync(Arg.Any<SubmitEventReportDto>(), Arg.Any<CancellationToken>());
        await Assert.That(GetPrivateField<string?>(cut.Instance, "_errorMessage")).IsEqualTo("Add details before submitting.");
    }

    public void Dispose() => _ctx.Dispose();

    private IRenderedComponent<ReportEventDialog> RenderDialog(Guid eventId) =>
        _ctx.RenderMudComponent<ReportEventDialog>(parameters => parameters
            .Add(component => component.EventId, eventId)
            .Add(component => component.EventTitle, "Community Program"));

    private IEventReportingService RegisterReportingService(Guid eventId)
    {
        var service = Substitute.For<IEventReportingService>();
        service.GetOptionsAsync(eventId, Arg.Any<CancellationToken>())
            .Returns(new HalResourceOfEventReportOptionsDto
            {
                EventId = eventId,
                IsReportable = true,
                MaxReporterTextLength = 4000,
                ReasonOptions =
                [
                    new ReasonOptions2
                    {
                        ReasonId = 1,
                        ReasonCode = "spam",
                        ReasonName = "Spam",
                        Description = "Misleading, repetitive, or promotional content."
                    }
                ]
            });

        _ctx.Services.AddSingleton(service);
        return service;
    }

    private static T GetPrivateProperty<T>(object instance, string propertyName)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Property {propertyName} was not found.");

        return (T?)property.GetValue(instance)
            ?? throw new InvalidOperationException($"Property {propertyName} returned null.");
    }

    private static T GetPrivateField<T>(object instance, string fieldName)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Field {fieldName} was not found.");

        return (T?)field.GetValue(instance)
            ?? throw new InvalidOperationException($"Field {fieldName} returned null.");
    }

    private static void SetPrivateField<T>(object instance, string fieldName, T value)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Field {fieldName} was not found.");

        field.SetValue(instance, value);
    }

    private static async Task InvokePrivateTaskAsync(object instance, string methodName)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Method {methodName} was not found.");

        var task = method.Invoke(instance, null) as Task
            ?? throw new InvalidOperationException($"Method {methodName} did not return a task.");
        await task;
    }
}
