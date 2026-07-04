// ABOUTME: Models command outcomes returned by host-provided control-plane service adapters.
// ABOUTME: Keeps mutation feedback explicit and HAL-aware without coupling shared components to generated clients.

namespace Event.ControlPlane.Client.Contracts;

public sealed record ControlPlaneCommandResult(
    bool Success,
    string Message,
    string? FailureCode = null,
    int? StatusCode = null,
    IReadOnlyList<string>? Errors = null,
    IReadOnlyDictionary<string, ControlPlaneHalLink>? Links = null)
{
    public IReadOnlyList<string> Errors { get; init; } = Errors ?? [];

    public IReadOnlyDictionary<string, ControlPlaneHalLink> Links { get; init; } = Links ?? ControlPlaneHal.EmptyLinks;

    public static ControlPlaneCommandResult Succeeded(
        string message,
        IReadOnlyDictionary<string, ControlPlaneHalLink>? links = null) =>
        new(true, message, Links: links);

    public static ControlPlaneCommandResult Failed(
        string message,
        string? failureCode = null,
        int? statusCode = null,
        IEnumerable<string>? errors = null,
        IReadOnlyDictionary<string, ControlPlaneHalLink>? links = null) =>
        new(false, message, failureCode, statusCode, errors?.ToArray() ?? [], links);
}
