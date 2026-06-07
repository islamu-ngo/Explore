// ABOUTME: Aggregates advisory AI evaluation scenario outcomes for trend reports.
// ABOUTME: Marks the report non-gating so normal CI remains free of live provider calls.

namespace Explore.Diagnostic.AiEvaluation;

public sealed record AiEvaluationReport(
    DateTimeOffset GeneratedAtUtc,
    string ReportVersion,
    bool AdvisoryOnly,
    IReadOnlyList<AiEvaluationScenarioResult> Results)
{
    public int PassCount => Results.Count(result => result.Status == AiEvaluationStatus.Pass);
    public int WarnCount => Results.Count(result => result.Status == AiEvaluationStatus.Warn);
    public int FailCount => Results.Count(result => result.Status == AiEvaluationStatus.Fail);
    public bool ContainsHardCiGate => false;
}
