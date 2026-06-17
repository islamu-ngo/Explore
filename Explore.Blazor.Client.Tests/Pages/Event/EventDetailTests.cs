// ABOUTME: Component tests for EventDetail display helper behavior.
// ABOUTME: Verifies storage-backed event images render when API responses include an image id without a resolved URI.

using System.Reflection;
using Explore.Blazor.Client.Pages.Events;

namespace Explore.Blazor.Client.Tests.Pages.Event;

public sealed class EventDetailTests : IDisposable
{
    private readonly BlazorTestContext _ctx = new();

    [Test]
    public async Task GetImageUrl_WhenFeaturedImageUriMissing_UsesPublicStorageObjectUrl()
    {
        var imageId = Guid.NewGuid();
        var component = new EventDetail();
        SetProperty(component, "Navigation", _ctx.Services.GetRequiredService<NavigationManager>());
        SetField(component, "_eventDetails", new EventDto
        {
            Id = Guid.NewGuid(),
            FeaturedImageId = imageId,
            FeaturedImageUri = null
        });

        var imageUrl = InvokePrivate<string?>(component, "GetImageUrl");

        await Assert.That(imageUrl).IsNotNull();
        await Assert.That(imageUrl!).EndsWith($"/api/storageobject/{imageId}/content");
    }

    public void Dispose() => _ctx.Dispose();

    private static T InvokePrivate<T>(object instance, string methodName)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Method {methodName} was not found.");

        return (T?)method.Invoke(instance, null)
            ?? throw new InvalidOperationException($"Method {methodName} returned null.");
    }

    private static void SetField<T>(object instance, string fieldName, T value)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Field {fieldName} was not found.");
        field.SetValue(instance, value);
    }

    private static void SetProperty<T>(object instance, string propertyName, T value)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Property {propertyName} was not found.");
        property.SetValue(instance, value);
    }
}
