// ABOUTME: Models revision-bound workstream facts and legal phase transitions.
// ABOUTME: Exposes closed typed validation errors instead of unstructured failure text.

namespace ISLAMU.AgentWorkflow.Domain;

public enum WorkstreamErrorCode
{
    StaleArtifactDigest,
    CommitAuthorityRequired,
    IllegalTransition,
    PhasePacketIncomplete,
    UnknownField,
    UnsafePath,
    ExpectedHeadMismatch,
}

public sealed record WorkstreamError(WorkstreamErrorCode Code);

public sealed record ArtifactBinding(string Path, string Sha256);

public sealed record ArtifactBindings(
    ArtifactBinding Plan,
    ArtifactBinding Tasks,
    ArtifactBinding Ivsd,
    ArtifactBinding CtoReview)
{
    public IEnumerable<ArtifactBinding> InRevisionOrder()
    {
        yield return Plan;
        yield return Tasks;
        yield return Ivsd;
        yield return CtoReview;
    }
}

public sealed record ApprovalBinding(string Decision, string RevisionDigest);

public sealed record ApprovalBindings(ApprovalBinding Cto, ApprovalBinding UserImplementation);

public sealed record PhaseCommitAuthority(
    string Decision,
    string PhaseId,
    string RevisionDigest,
    string ExpectedHead);

public sealed record CommitPacket(string Type, string Scope, string Changelog, IReadOnlyList<string> Trailers);

public sealed record PhasePacket(
    IReadOnlyList<string> Paths,
    IReadOnlyList<string> VerificationCommands,
    CommitPacket? Commit);

public sealed record CurrentPhase(string Id, string State, string RequestedTransition, PhasePacket Packet);

public sealed record WorkstreamExecution(
    string SchemaVersion,
    string WorkstreamId,
    ArtifactBindings Artifacts,
    string RevisionDigest,
    ApprovalBindings Approvals,
    PhaseCommitAuthority? PhaseCommit,
    string ExpectedHead,
    CurrentPhase CurrentPhase)
{
    public WorkstreamError? ValidateRequestedTransition()
    {
        return string.Equals(CurrentPhase.State, "approved", StringComparison.Ordinal) &&
               string.Equals(CurrentPhase.RequestedTransition, "implementing", StringComparison.Ordinal)
            ? null
            : new WorkstreamError(WorkstreamErrorCode.IllegalTransition);
    }
}

public sealed record ValidatedWorkstream(
    string WorkstreamId,
    string PhaseId,
    string CurrentState,
    string NextTransition,
    IReadOnlyList<string> Paths,
    IReadOnlyList<string> VerificationCommands);

public sealed record WorkstreamResult(ValidatedWorkstream? Value, WorkstreamError? Error)
{
    public static WorkstreamResult Success(ValidatedWorkstream value) => new(value, null);

    public static WorkstreamResult Failure(WorkstreamErrorCode code) => new(null, new WorkstreamError(code));
}
