namespace Explore.API.Hateoas;

/// <summary>
/// Constants used in HATEOAS implementation.
/// </summary>
public static class HateoasConstants
{
    /// <summary>
    /// The key used in HttpContext.Items to store the minimal response preference.
    /// </summary>
    public const string MinimalResponseKey = "Hateoas:ReturnMinimal";

    /// <summary>
    /// The HAL+JSON media type.
    /// </summary>
    public const string HalJsonMediaType = "application/hal+json";

    /// <summary>
    /// The standard JSON media type.
    /// </summary>
    public const string JsonMediaType = "application/json";

    /// <summary>
    /// RFC 7240 Prefer header name.
    /// </summary>
    public const string PreferHeader = "Prefer";

    /// <summary>
    /// RFC 7240 Preference-Applied response header name.
    /// </summary>
    public const string PreferenceAppliedHeader = "Preference-Applied";

    /// <summary>
    /// RFC 7240 return=minimal preference value.
    /// </summary>
    public const string ReturnMinimal = "return=minimal";

    /// <summary>
    /// RFC 7240 return=representation preference value.
    /// </summary>
    public const string ReturnRepresentation = "return=representation";
}
