// ABOUTME: Application contract for the platform scheduled-job catalog.
// ABOUTME: Lets health checks and operator APIs reason about known jobs without depending on TickerQ.

namespace Explore.Application.Contracts.Scheduling;

public interface IScheduledJobRegistry
{
    IReadOnlyCollection<ScheduledJobDescriptor> ListJobs();

    ScheduledJobDescriptor? FindByName(string name);
}
