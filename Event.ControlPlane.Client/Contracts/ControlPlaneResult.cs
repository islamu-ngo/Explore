// ABOUTME: Defines service-result primitives for shared control-plane client contracts.
// ABOUTME: Models failure state safely without exposing transport exceptions or provider details to components.

namespace Event.ControlPlane.Client.Contracts;

public sealed record ControlPlaneResult<T>(
    ControlPlaneResultKind Kind,
    T? Value = default,
    ControlPlaneProblem? Problem = null)
{
    public bool IsSuccess => Kind == ControlPlaneResultKind.Success;
}

public static class ControlPlaneResult
{
    public static ControlPlaneResult<T> Success<T>(T value) =>
        new(ControlPlaneResultKind.Success, value);

    public static ControlPlaneResult<T> Failure<T>(
        ControlPlaneResultKind kind,
        ControlPlaneProblem problem) =>
        new(kind, default, problem);
}

public enum ControlPlaneResultKind
{
    Success = 0,
    NotConfigured = 1,
    NotFound = 2,
    Unauthenticated = 3,
    Forbidden = 4,
    ValidationFailed = 5,
    Conflict = 6,
    RateLimited = 7,
    Unavailable = 8,
    Failed = 9
}

public sealed record ControlPlaneProblem(
    string Code,
    string Message,
    int? StatusCode = null,
    IReadOnlyList<string>? Errors = null)
{
    public IReadOnlyList<string> Errors { get; init; } = Errors ?? [];
}
