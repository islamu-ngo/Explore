namespace Explore.Blazor.Client.Helpers;

/// <summary>
/// Provides utility methods for consistent DateTimeOffset conversions to UTC.
/// PostgreSQL timestamptz columns require DateTimeOffset with offset 0 (UTC).
/// </summary>
public static class DateTimeHelper
{
    /// <summary>
    /// Converts a local DateTime to UTC DateTimeOffset (offset 0).
    /// This is required for PostgreSQL timestamptz compatibility.
    /// </summary>
    /// <param name="localDateTime">The local DateTime value from UI date/time pickers</param>
    /// <returns>DateTimeOffset in UTC with offset 0</returns>
    /// <remarks>
    /// Assumes the input DateTime is in the local timezone.
    /// PostgreSQL timestamptz requires UTC (offset 0) - this method ensures compliance.
    /// </remarks>
    public static DateTimeOffset ConvertLocalToUtc(DateTime localDateTime)
    {
        // Create DateTimeOffset with local timezone offset, then convert to UTC
        var localOffset = TimeZoneInfo.Local.GetUtcOffset(localDateTime);
        var dateTimeOffset = new DateTimeOffset(localDateTime, localOffset);
        return dateTimeOffset.ToUniversalTime();
    }

    /// <summary>
    /// Converts a nullable local DateTime to UTC DateTimeOffset (offset 0).
    /// Returns null if input is null.
    /// </summary>
    /// <param name="localDateTime">The nullable local DateTime value</param>
    /// <returns>Nullable DateTimeOffset in UTC with offset 0</returns>
    public static DateTimeOffset? ConvertLocalToUtc(DateTime? localDateTime)
    {
        return localDateTime.HasValue ? ConvertLocalToUtc(localDateTime.Value) : null;
    }

    /// <summary>
    /// Combines nullable date and time parts into UTC DateTimeOffset.
    /// Returns null if either date or time is null.
    /// </summary>
    /// <param name="date">The date part (from MudDatePicker)</param>
    /// <param name="time">The time part (from MudTimePicker)</param>
    /// <returns>Combined DateTimeOffset in UTC with offset 0, or null if either input is null</returns>
    public static DateTimeOffset? CombineDateTimeToUtc(DateTime? date, TimeSpan? time)
    {
        if (!date.HasValue || !time.HasValue)
            return null;

        var combined = date.Value.Date + time.Value;
        return ConvertLocalToUtc(combined);
    }

    /// <summary>
    /// Converts UTC DateTimeOffset to local DateTime for display in UI.
    /// Used when populating date/time pickers from database values.
    /// </summary>
    /// <param name="utcDateTimeOffset">The UTC DateTimeOffset from database</param>
    /// <returns>Local DateTime for UI display</returns>
    public static DateTime ConvertUtcToLocal(DateTimeOffset utcDateTimeOffset)
    {
        return utcDateTimeOffset.LocalDateTime;
    }

    /// <summary>
    /// Converts nullable UTC DateTimeOffset to nullable local DateTime.
    /// Returns null if input is null.
    /// </summary>
    /// <param name="utcDateTimeOffset">The nullable UTC DateTimeOffset</param>
    /// <returns>Nullable local DateTime for UI display</returns>
    public static DateTime? ConvertUtcToLocal(DateTimeOffset? utcDateTimeOffset)
    {
        return utcDateTimeOffset?.LocalDateTime;
    }
}
