// ABOUTME: Owns the single configuration input for approved-registration reminder lead time.
// ABOUTME: Provides startup-compatible range validation plus a defensive runtime conversion.

namespace Explore.Application.Configuration;

public sealed class EventReminderOptions
{
    public const string SectionName = "EmailDispatch";
    public const int DefaultLeadTimeHours = 24;
    public const int MinLeadTimeHours = 1;
    public const int MaxLeadTimeHours = 168;

    public int EventReminderLeadTimeHours { get; set; } = DefaultLeadTimeHours;

    public static bool IsValidLeadTimeHours(int hours) =>
        hours is >= MinLeadTimeHours and <= MaxLeadTimeHours;

    public TimeSpan GetValidatedLeadTime()
    {
        if (!IsValidLeadTimeHours(EventReminderLeadTimeHours))
        {
            throw new InvalidOperationException(
                $"EmailDispatch:EventReminderLeadTimeHours must be between {MinLeadTimeHours} and {MaxLeadTimeHours} hours.");
        }

        return TimeSpan.FromHours(EventReminderLeadTimeHours);
    }
}
