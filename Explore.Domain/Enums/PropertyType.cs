// ABOUTME: Enum defining the data types supported by custom property definitions.
// ABOUTME: Determines which typed value column is used in CustomPropertyValue.

namespace Explore.Domain.Enums;

public enum PropertyType
{
    Text = 1,
    Number = 2,
    Option = 3,
    Boolean = 4,
    DateTime = 5,
    Url = 6
}
