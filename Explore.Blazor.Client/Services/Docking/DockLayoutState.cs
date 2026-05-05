// ABOUTME: Scoped UI state engine for generic shell and workspace dock panels.
// ABOUTME: Manages registration, open/close, sizing, activation, and layout snapshots.

using Microsoft.AspNetCore.Components;

namespace Explore.Blazor.Client.Services.Docking;

public sealed class DockLayoutState : IDockPanelRegistry
{
    private const int MinimumMobileContentWidth = 375;

    private readonly Dictionary<DockPanelId, DockPanelEntry> _entries = [];
    private int _viewportWidth;
    private bool _isMobileViewport;

    public event Action? Changed;

    public DockLayoutChangeReason LastChangeReason { get; private set; } = DockLayoutChangeReason.None;

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
        NotifyChanged(DockLayoutChangeReason.Registration);
    }

    public void Unregister(DockPanelId id)
    {
        if (_entries.Remove(id))
        {
            NotifyChanged(DockLayoutChangeReason.Registration);
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

    public bool ShouldRenderDockedPanelAsOverlay(DockPanelEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (entry.State is not { IsOpen: true, Mode: DockMode.Docked })
        {
            return false;
        }

        if (_isMobileViewport)
        {
            return true;
        }

        if (_viewportWidth <= 0 || entry.Descriptor.Side != DockSide.Start)
        {
            return false;
        }

        var dockedEndWidth = _entries.Values
            .Where(candidate => candidate.State is { IsOpen: true, Mode: DockMode.Docked }
                && candidate.Descriptor.Side == DockSide.End)
            .Sum(candidate => candidate.State.Width);
        var availableContentWidth = _viewportWidth - dockedEndWidth - entry.State.Width;

        return availableContentWidth < MinimumMobileContentWidth;
    }

    public void Refresh()
    {
        NotifyChanged(DockLayoutChangeReason.Refresh);
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

        changed |= ApplyResponsivePolicy(preferredPanelId: id);

        if (changed)
        {
            NotifyChanged(DockLayoutChangeReason.UserAction);
        }
    }

    public void UpdateViewport(int viewportWidth, bool isMobile)
    {
        var normalizedWidth = Math.Max(0, viewportWidth);
        var viewportChanged = _viewportWidth != normalizedWidth || _isMobileViewport != isMobile;

        _viewportWidth = normalizedWidth;
        _isMobileViewport = isMobile;

        var stateChanged = ApplyResponsivePolicy(preferredPanelId: null);

        if (viewportChanged || stateChanged)
        {
            NotifyChanged(DockLayoutChangeReason.ViewportPolicy);
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
            NotifyChanged(DockLayoutChangeReason.UserAction);
        }
    }

    public DockLayoutSnapshot CreateSnapshot(string layoutKey, DockScope scope)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(layoutKey);

        var panels = _entries.Values
            .Where(entry => entry.Descriptor.Scope == scope && entry.Descriptor.PersistState)
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
            NotifyChanged(DockLayoutChangeReason.Reset);
        }
    }

    public void RestoreSnapshot(DockLayoutSnapshot snapshot, string layoutKey, DockScope scope)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentException.ThrowIfNullOrWhiteSpace(layoutKey);

        if (!string.Equals(snapshot.LayoutKey, layoutKey, StringComparison.Ordinal))
        {
            return;
        }

        var restoredAny = false;
        var nextStates = _entries.ToDictionary(entry => entry.Key, entry => entry.Value.State);

        foreach (var panelState in snapshot.Panels)
        {
            if (!_entries.TryGetValue(panelState.Id, out var entry)
                || entry.Descriptor.Scope != scope
                || !entry.Descriptor.PersistState)
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
            NotifyChanged(DockLayoutChangeReason.SnapshotRestore);
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

    private bool ApplyResponsivePolicy(DockPanelId? preferredPanelId)
    {
        if (_viewportWidth <= 0)
        {
            return false;
        }

        var changed = false;

        changed |= CloseStartPanelsWhenContentIsConstrained(preferredPanelId);
        changed |= EnforceSingleEndPanelWhenConstrained(preferredPanelId);

        return changed;
    }

    private bool CloseStartPanelsWhenContentIsConstrained(DockPanelId? preferredPanelId)
    {
        if (_isMobileViewport)
        {
            return CloseOpenPanels(entry => entry.Descriptor.Side == DockSide.Start
                && entry.Descriptor.Id != preferredPanelId
                && entry.Descriptor.CanClose);
        }

        var dockedInlineWidth = _entries.Values
            .Where(entry => entry.State is { IsOpen: true, Mode: DockMode.Docked })
            .Sum(entry => entry.State.Width);
        var availableContentWidth = _viewportWidth - dockedInlineWidth;

        if (availableContentWidth >= MinimumMobileContentWidth)
        {
            return false;
        }

        return CloseOpenPanels(entry => entry.Descriptor.Side == DockSide.Start
            && entry.State.Mode == DockMode.Docked
            && entry.Descriptor.Id != preferredPanelId
            && entry.Descriptor.CanClose);
    }

    private bool EnforceSingleEndPanelWhenConstrained(DockPanelId? preferredPanelId)
    {
        var openEndPanels = _entries.Values
            .Where(entry => entry.State.IsOpen && entry.Descriptor.Side == DockSide.End)
            .OrderBy(entry => entry.State.Order)
            .ThenBy(entry => entry.Descriptor.Title, StringComparer.Ordinal)
            .ToArray();

        if (openEndPanels.Length <= 1)
        {
            return false;
        }

        var endPanelWidth = openEndPanels
            .Where(entry => entry.State.Mode == DockMode.Docked)
            .Sum(entry => entry.State.Width);
        var remainingContentWidth = _viewportWidth - endPanelWidth;

        if (!_isMobileViewport && remainingContentWidth >= MinimumMobileContentWidth)
        {
            return false;
        }

        var panelToKeep = preferredPanelId is not null
            ? openEndPanels.FirstOrDefault(entry => entry.Descriptor.Id == preferredPanelId)
            : null;
        panelToKeep ??= openEndPanels.LastOrDefault(entry => entry.State.IsActive);
        panelToKeep ??= openEndPanels.Last();

        return CloseOpenPanels(entry => entry.Descriptor.Side == DockSide.End
            && entry.Descriptor.Id != panelToKeep.Descriptor.Id
            && entry.Descriptor.CanClose);
    }

    private bool CloseOpenPanels(Func<DockPanelEntry, bool> shouldClose)
    {
        var changed = false;

        foreach (var entry in _entries.Values.ToArray())
        {
            if (!entry.State.IsOpen || !shouldClose(entry))
            {
                continue;
            }

            SetEntryState(entry.Descriptor.Id, entry.State with { IsOpen = false, IsActive = false });
            changed = true;
        }

        return changed;
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
        NotifyChanged(DockLayoutChangeReason.UserAction);
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

    private void NotifyChanged(DockLayoutChangeReason reason)
    {
        LastChangeReason = reason;
        Changed?.Invoke();
    }
}
