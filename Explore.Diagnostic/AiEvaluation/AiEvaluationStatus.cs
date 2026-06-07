// ABOUTME: Represents advisory AI evaluation scenario status values.
// ABOUTME: Allows reports to distinguish regressions from trend warnings without creating CI gates.

namespace Explore.Diagnostic.AiEvaluation;

public enum AiEvaluationStatus
{
    Pass,
    Warn,
    Fail,
}
