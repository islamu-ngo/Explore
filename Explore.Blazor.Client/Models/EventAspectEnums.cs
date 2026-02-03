// ABOUTME: Domain enum definitions for event aspects (Islamic and Tech)
// ABOUTME: These mirror the Domain layer enums for use in the Blazor client

namespace Explore.Blazor.Client.Models;

/// <summary>
/// Prayer times for scheduling events relative to prayers.
/// </summary>
public enum PrayerTime
{
    Fajr = 1,
    Sunrise = 2,
    Dhuhr = 3,
    Asr = 4,
    Maghrib = 5,
    Isha = 6
}

/// <summary>
/// Gender segregation modes for Islamic events.
/// </summary>
public enum GenderSegregationMode
{
    /// <summary>Mixed attendance (no segregation).</summary>
    Mixed = 0,
    /// <summary>Men only event.</summary>
    MenOnly = 1,
    /// <summary>Women only event.</summary>
    WomenOnly = 2,
    /// <summary>Segregated sections for men and women.</summary>
    Segregated = 3,
    /// <summary>Family-oriented with family seating.</summary>
    Family = 4
}

/// <summary>
/// Skill level requirements for tech events.
/// </summary>
public enum SkillLevel
{
    /// <summary>All skill levels welcome.</summary>
    AllLevels = 0,
    /// <summary>Beginner-friendly content.</summary>
    Beginner = 1,
    /// <summary>Intermediate knowledge required.</summary>
    Intermediate = 2,
    /// <summary>Advanced/expert level content.</summary>
    Advanced = 3
}
