// ABOUTME: Scoped UI state engine for generic shell and workspace dock panels.
// ABOUTME: Manages registration, open/close, sizing, activation, and layout snapshots.

using Microsoft.AspNetCore.Components;

namespace Explore.Blazor.Client.Services.Docking;

public sealed class DockLayoutState : IDockPanelRegistry
{
    private readonly Dictionary<DockPanelId, DockPanelEntry> _entries = [];

    public event Action? Changed;

    public void Register(DockPanelDescriptor descriptor, RenderFragment content)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(content);

        descriptor = descriptor.Validate();

        if (_entries.ContainsKey(descriptor.Id))
        {
            throw new InvalidOperationException($"Dock panel '{descriptor.Id}' is already registered.");
        }

        var state = new DockPanelState(
            descriptor.Id,
            IsOpen: false,
            descriptor.DefaultMode,
            descriptor.DefaultWidth,
            descriptor.Order,
            IsActive: false);

        _entries.Add(descriptor.Id, new DockPanelEntry(descriptor, content, state));
        NotifyChanged();
    }

    public void Unregister(DockPanelId id)
    {
        if (_entries.Remove(id))
        {
            NotifyChanged();
        }
    }

    public IReadOnlyList<DockPanelEntry> GetPanels(DockScope scope, DockSide side)
    {
        return _entries.Values
            .Where(entry => entry.Descriptor.Scope == scope && entry.Descriptor.Side == side)
            .OrderBy(entry => entry.State.Order)
            .ThenBy(entry => entry.Descriptor.Title, StringComparer.Ordinal)
            .ToArray();
    }

    public DockPanelEntry? GetPanel(DockPanelId id)
    {
        return _entries.GetValueOrDefault(id);
    }

    public void Refresh()
    {
        NotifyChanged();
    }

    public void Open(DockPanelId id)
    {
        var activeEntry = RequireEntry(id);
        var changed = false;

        foreach (var entry in _entries.Values.ToArray())
        {
            if (entry.Descriptor.Scope != activeEntry.Descriptor.Scope
                || entry.Descriptor.Side != activeEntry.Descriptor.Side)
            {
                continue;
            }

            var shouldBeOpen = entry.Descriptor.Id == id || entry.State.IsOpen;
            var shouldBeActive = entry.Descriptor.Id == id;
            var nextState = entry.State with
            {
                IsOpen = shouldBeOpen,
                IsActive = shouldBeActive
            };

            if (entry.State == nextState)
            {
                continue;
            }

            SetEntryState(entry.Descriptor.Id, nextState);
            changed = true;
        }

        if (changed)
        {
            NotifyChanged();
        }
    }

    public void Close(DockPanelId id)
    {
        var entry = RequireEntry(id);

        if (!entry.Descriptor.CanClose)
        {
            throw new InvalidOperationException($"Dock panel '{id}' cannot be closed.");
        }

        UpdateState(id, state => state with { IsOpen = false, IsActive = false });
    }

    public void Toggle(DockPanelId id)
    {
        var entry = RequireEntry(id);

        if (entry.State.IsOpen)
        {
            Close(id);
            return;
        }

        Open(id);
    }

    public void SetMode(DockPanelId id, DockMode mode)
    {
        UpdateState(id, state => state with { Mode = mode });
    }

    public void Resize(DockPanelId id, int width)
    {
        var entry = RequireEntry(id);

        if (!entry.Descriptor.IsResizable)
        {
            throw new InvalidOperationException($"Dock panel '{id}' cannot be resized.");
        }

        var clampedWidth = Math.Clamp(width, entry.Descriptor.MinWidth, entry.Descriptor.MaxWidth);

        UpdateState(id, state => state with { Width = clampedWidth });
    }

    public void Activate(DockPanelId id)
    {
        var activeEntry = RequireEntry(id);
        var changed = false;

        foreach (var entry in _entries.Values.ToArray())
        {
            if (entry.Descriptor.Scope != activeEntry.Descriptor.Scope
                || entry.Descriptor.Side != activeEntry.Descriptor.Side)
            {
                continue;
            }

            var shouldBeActive = entry.Descriptor.Id == id;
            var nextState = entry.State with { IsActive = shouldBeActive };

            if (entry.State == nextState)
            {
                continue;
            }

            SetEntryState(entry.Descriptor.Id, nextState);
            changed = true;
        }

        if (changed)
        {
            NotifyChanged();
        }
    }

    public DockLayoutSnapshot CreateSnapshot(string layoutKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(layoutKey);

        var panels = _entries.Values
            .Where(entry => entry.Descriptor.PersistState)
            .OrderBy(entry => entry.Descriptor.Scope)
            .ThenBy(entry => entry.Descriptor.Side)
            .ThenBy(entry => entry.State.Order)
            .Select(entry => entry.State)
            .ToArray();

        return new DockLayoutSnapshot(layoutKey, panels, DateTimeOffset.UtcNow);
    }

    public void ResetToDefaults()
    {
        var changed = false;

        foreach (var entry in _entries.Values.ToArray())
        {
            var defaultState = CreateDefaultState(entry.Descriptor);

            if (entry.State == defaultState)
            {
                continue;
            }

            SetEntryState(entry.Descriptor.Id, defaultState);
            changed = true;
        }

        if (changed)
        {
            NotifyChanged();
        }
    }

    public void RestoreSnapshot(DockLayoutSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var restoredAny = false;
        var nextStates = _entries.ToDictionary(entry => entry.Key, entry => entry.Value.State);

        foreach (var panelState in snapshot.Panels)
        {
            if (!_entries.TryGetValue(panelState.Id, out var entry) || !entry.Descriptor.PersistState)
            {
                continue;
            }

            var currentState = nextStates[panelState.Id];
            var width = entry.Descriptor.IsResizable
                ? Math.Clamp(panelState.Width, entry.Descriptor.MinWidth, entry.Descriptor.MaxWidth)
                : currentState.Width;
            var isOpen = panelState.IsOpen || (!entry.Descriptor.CanClose && currentState.IsOpen);

            nextStates[panelState.Id] = panelState with
            {
                IsOpen = isOpen,
                Width = width,
                IsActive = isOpen && panelState.IsActive
            };
            restoredAny = true;
        }

        if (!restoredAny)
        {
            return;
        }

        NormalizeActiveStates(nextStates);

        var changed = false;

        foreach (var entry in _entries.Values.ToArray())
        {
            var nextState = nextStates[entry.Descriptor.Id];

            if (entry.State == nextState)
            {
                continue;
            }

            SetEntryState(entry.Descriptor.Id, nextState);
            changed = true;
        }

        if (changed)
        {
            NotifyChanged();
        }
    }

    private void NormalizeActiveStates(Dictionary<DockPanelId, DockPanelState> nextStates)
    {
        foreach (var id in nextStates.Keys.ToArray())
        {
            var state = nextStates[id];

            if (state is { IsOpen: false, IsActive: true })
            {
                nextStates[id] = state with { IsActive = false };
            }
        }

        foreach (var group in _entries.Values.GroupBy(entry => new { entry.Descriptor.Scope, entry.Descriptor.Side }))
        {
            var openEntries = group
                .Where(entry => nextStates[entry.Descriptor.Id].IsOpen)
                .OrderBy(entry => nextStates[entry.Descriptor.Id].Order)
                .ThenBy(entry => entry.Descriptor.Title, StringComparer.Ordinal)
                .ToArray();

            var activeEntries = openEntries
                .Where(entry => nextStates[entry.Descriptor.Id].IsActive)
                .ToArray();

            if (activeEntries.Length == 0 && openEntries.Length > 0)
            {
                var firstOpenEntry = openEntries[0];
                var state = nextStates[firstOpenEntry.Descriptor.Id];
                nextStates[firstOpenEntry.Descriptor.Id] = state with { IsActive = true };
                continue;
            }

            foreach (var entry in activeEntries.Skip(1))
            {
                var state = nextStates[entry.Descriptor.Id];
                nextStates[entry.Descriptor.Id] = state with { IsActive = false };
            }
        }
    }

    private DockPanelEntry RequireEntry(DockPanelId id)
    {
        return _entries.TryGetValue(id, out var entry)
            ? entry
            : throw new KeyNotFoundException($"Dock panel '{id}' is not registered.");
    }

    private void UpdateState(DockPanelId id, Func<DockPanelState, DockPanelState> update)
    {
        var entry = RequireEntry(id);
        var nextState = update(entry.State);

        if (entry.State == nextState)
        {
            return;
        }

        SetEntryState(id, nextState);
        NotifyChanged();
    }

    private void SetEntryState(DockPanelId id, DockPanelState state)
    {
        var entry = RequireEntry(id);
        _entries[id] = entry with { State = state };
    }

    private static DockPanelState CreateDefaultState(DockPanelDescriptor descriptor)
    {
        return new DockPanelState(
            descriptor.Id,
            IsOpen: false,
            descriptor.DefaultMode,
            descriptor.DefaultWidth,
            descriptor.Order,
            IsActive: false);
    }

    private void NotifyChanged() => Changed?.Invoke();
}
