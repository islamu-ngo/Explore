// ABOUTME: Represents the fail-closed Windows protected-write disposition until ACL evidence exists.
// ABOUTME: Never inherits ambient permissions or falls back to an unprotected plaintext write.

namespace ISLAMU.Event.SetupAssistant.Desktop.Files;

public sealed class WindowsProtectedFileWriter : IProtectedFileWriter
{
    public bool IsAvailable => false;

    public Task<ProtectedWritePreparation> PrepareAsync(
        ProtectedWriteRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(
            ProtectedWritePreparation.FromResult(ProtectedWriteResult.Unsupported()));
    }
}
