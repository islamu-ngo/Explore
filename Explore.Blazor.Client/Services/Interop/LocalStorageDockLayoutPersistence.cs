// ABOUTME: localStorage-backed dock layout persistence for browser-only dock preferences.
// ABOUTME: Stores schema-versioned snapshots behind IDockLayoutPersistence and fails soft during prerender or corrupt data.

using System.Text.Json;
using System.Text.Json.Serialization;
using Explore.Blazor.Client.Services.Docking;
using Microsoft.JSInterop;

namespace Explore.Blazor.Client.Services.Interop;

public sealed class LocalStorageDockLayoutPersistence : IDockLayoutPersistence, IAsyncDisposable
{
    private const int CurrentSchemaVersion = 1;
    private const string JsModulePath = "/js/dock-layout-persistence.js";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<LocalStorageDockLayoutPersistence> _logger;
    private IJSObjectReference? _jsModule;

    public LocalStorageDockLayoutPersistence(
        IJSRuntime jsRuntime,
        ILogger<LocalStorageDockLayoutPersistence> logger)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    public async Task<DockLayoutSnapshot?> LoadAsync(string layoutKey, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(layoutKey);

        if (!OperatingSystem.IsBrowser())
        {
            return null;
        }

        try
        {
            var module = await GetJsModuleAsync(cancellationToken);
            var json = await module.InvokeAsync<string?>("get", cancellationToken, layoutKey);

            return Deserialize(layoutKey, json, _logger);
        }
        catch (Exception ex) when (ex is JSException or InvalidOperationException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Failed to load dock layout snapshot for '{LayoutKey}'", layoutKey);
            return null;
        }
    }

    public async Task<bool> SaveAsync(DockLayoutSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentException.ThrowIfNullOrWhiteSpace(snapshot.LayoutKey);

        if (!OperatingSystem.IsBrowser())
        {
            return false;
        }

        try
        {
            var module = await GetJsModuleAsync(cancellationToken);
            await module.InvokeVoidAsync("set", cancellationToken, snapshot.LayoutKey, Serialize(snapshot));
            return true;
        }
        catch (Exception ex) when (ex is JSException or InvalidOperationException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Failed to save dock layout snapshot for '{LayoutKey}'", snapshot.LayoutKey);
            return false;
        }
    }

    public async Task<bool> DeleteAsync(string layoutKey, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(layoutKey);

        if (!OperatingSystem.IsBrowser())
        {
            return false;
        }

        try
        {
            var module = await GetJsModuleAsync(cancellationToken);
            await module.InvokeVoidAsync("remove", cancellationToken, layoutKey);
            return true;
        }
        catch (Exception ex) when (ex is JSException or InvalidOperationException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Failed to delete dock layout snapshot for '{LayoutKey}'", layoutKey);
            return false;
        }
    }

    internal static string Serialize(DockLayoutSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentException.ThrowIfNullOrWhiteSpace(snapshot.LayoutKey);

        var envelope = new DockLayoutStorageEnvelope(CurrentSchemaVersion, snapshot.LayoutKey, snapshot);
        return JsonSerializer.Serialize(envelope, JsonOptions);
    }

    internal static DockLayoutSnapshot? Deserialize(
        string layoutKey,
        string? json,
        ILogger logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(layoutKey);
        ArgumentNullException.ThrowIfNull(logger);

        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            var envelope = JsonSerializer.Deserialize<DockLayoutStorageEnvelope>(json, JsonOptions);

            if (envelope is null
                || envelope.SchemaVersion != CurrentSchemaVersion
                || !string.Equals(envelope.LayoutKey, layoutKey, StringComparison.Ordinal)
                || !string.Equals(envelope.Snapshot.LayoutKey, layoutKey, StringComparison.Ordinal))
            {
                return null;
            }

            return envelope.Snapshot;
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Ignored corrupt dock layout snapshot for '{LayoutKey}'", layoutKey);
            return null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_jsModule is not null)
            {
                await _jsModule.DisposeAsync();
                _jsModule = null;
            }
        }
        catch (JSDisconnectedException)
        {
            // Expected when Blazor circuit disconnects before disposal.
        }
    }

    private async ValueTask<IJSObjectReference> GetJsModuleAsync(CancellationToken cancellationToken)
    {
        return _jsModule ??= await _jsRuntime.InvokeAsync<IJSObjectReference>(
            "import",
            cancellationToken,
            JsModulePath);
    }

    private sealed record DockLayoutStorageEnvelope(
        int SchemaVersion,
        string LayoutKey,
        DockLayoutSnapshot Snapshot);
}
