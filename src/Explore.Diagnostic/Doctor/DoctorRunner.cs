// ABOUTME: Runs doctor checks with bounded execution and partial-result behavior.
// ABOUTME: Converts unexpected check failures into WARN results instead of hiding them.

namespace Explore.Diagnostic.Doctor;

public sealed class DoctorRunner(IEnumerable<IDoctorCheck> checks)
{
    public async Task<DoctorReport> RunAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout);

        var results = new List<DoctorCheckResult>();
        foreach (var check in checks)
        {
            try
            {
                results.Add(await check.RunAsync(timeoutSource.Token));
            }
            catch (OperationCanceledException) when (timeoutSource.IsCancellationRequested)
            {
                results.Add(DoctorCheckResult.Warn(
                    check.Code,
                    check.Category,
                    "The check timed out before it could complete.",
                    "Increase --timeout-seconds or verify the dependency manually; doctor did not mutate state.",
                    "docs/TROUBLESHOOTING.md"));
            }
            catch (Exception ex)
            {
                results.Add(DoctorCheckResult.Warn(
                    check.Code,
                    check.Category,
                    "The check failed unexpectedly before producing a result.",
                    "Review the redacted evidence and run the related manual remediation steps.",
                    "docs/TROUBLESHOOTING.md",
                    DoctorRedactor.Redact(ex.Message)));
            }
        }

        return new DoctorReport(results);
    }
}
