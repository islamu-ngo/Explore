// ABOUTME: Composes one secret-bearing dotenv through Setup Core and writes it only through protected output.
// ABOUTME: Returns value-free digest/readiness metadata and clears the owned rendered byte copy on every exit.

using System.Security.Cryptography;
using ISLAMU.Event.Setup.Core;
using ISLAMU.Event.Setup.Core.Environment;

namespace ISLAMU.Event.SetupAssistant.Cli.Tui;

internal sealed record SetupTerminalArtifactResult(
    SetupTerminalOutcome Outcome,
    string Code,
    string? Digest,
    SetupTerminalReadiness Readiness,
    int MissingCount,
    int BlockedCount,
    SetupTerminalProtectedWriteResult Write);

internal static class SetupTerminalArtifactComposer
{
    internal static SetupTerminalArtifactResult ComposeAndWrite(
        ISetupTerminalProtectedWriter writer,
        string validatedFileName,
        string transientSecret,
        DotenvEntryKind kind,
        DotenvProvenance provenance,
        int maximumBytes)
    {
        byte[] renderedBytes = [];
        try
        {
            var context = new EnvironmentActivationContext(
                "standalone", ["platform"], ["environment", "local", "sqlite"]);
            DotenvCompositionResult composition = DotenvComposer.ComposeWithSecrets(
                CanonicalEnvironmentCatalogue.Catalogue, context,
                [new DotenvEntry("SETUP_SECRET", transientSecret, kind, true, provenance)]);
            DotenvRenderResult rendered = DotenvCodec.Render(composition.Document, true);
            if (!rendered.Succeeded)
                return Result(SetupTerminalOutcome.Failed, "terminal-compose-failed", composition);

            renderedBytes = rendered.Bytes.ToArray();
            string digest = ArtifactDigest.Compute(renderedBytes).Value;
            SetupTerminalProtectedWriteResult write;
            try { write = writer.WriteCreateNew(validatedFileName, renderedBytes, maximumBytes); }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException)
            { write = SetupTerminalProtectedWriteResult.Blocked; }
            return write == SetupTerminalProtectedWriteResult.Written
                ? Result(SetupTerminalOutcome.Completed, "terminal-complete", composition, digest, write)
                : Result(SetupTerminalOutcome.Blocked, "protected-output-unavailable", composition, digest, write);
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException or ArgumentException)
        {
            return new SetupTerminalArtifactResult(SetupTerminalOutcome.Failed, "terminal-compose-failed", null,
                SetupTerminalReadiness.None, 0, 0, SetupTerminalProtectedWriteResult.Blocked);
        }
        finally
        {
            if (renderedBytes.Length > 0) CryptographicOperations.ZeroMemory(renderedBytes);
        }
    }

    private static SetupTerminalArtifactResult Result(
        SetupTerminalOutcome outcome,
        string code,
        DotenvCompositionResult composition,
        string? digest = null,
        SetupTerminalProtectedWriteResult write = SetupTerminalProtectedWriteResult.Blocked)
    {
        SetupTerminalReadiness readiness = composition.Readiness.State switch
        {
            DotenvReadinessState.Ready => SetupTerminalReadiness.Ready,
            DotenvReadinessState.Incomplete => SetupTerminalReadiness.Incomplete,
            DotenvReadinessState.Blocked => SetupTerminalReadiness.Blocked,
            _ => SetupTerminalReadiness.None,
        };
        return new SetupTerminalArtifactResult(outcome, code, digest, readiness,
            composition.Readiness.Missing.Count, composition.Readiness.Blocked.Count, write);
    }
}
