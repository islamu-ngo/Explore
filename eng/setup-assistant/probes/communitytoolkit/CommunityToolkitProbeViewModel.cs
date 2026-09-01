// ABOUTME: Exercises only the approved CommunityToolkit observable and command generator roles.
// ABOUTME: Contains no product behavior, secret value, target type, I/O, or runtime authority.

namespace ISLAMU.Event.SetupAssistant.Probes.CommunityToolkit;

using global::CommunityToolkit.Mvvm.ComponentModel;
using global::CommunityToolkit.Mvvm.Input;

internal sealed partial class CommunityToolkitProbeViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isReady;

    [RelayCommand]
    private void ToggleReady() => IsReady = !IsReady;
}
