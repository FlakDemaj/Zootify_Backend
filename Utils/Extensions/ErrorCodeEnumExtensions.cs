using System.ComponentModel;

namespace Zootify_Backend.Extensions;

/// <summary>
/// Provides an extension method for retrieving the description attribute of an enum value.
/// </summary>
public static class ErrorCodeEnumExtensions
{
    /// <summary>
    /// Retrieves the description of an enum value if it has a <see cref="DescriptionAttribute"/>.
    /// If no description is found, the enum value's name is returned as a string.
    /// </summary>
    /// <param name="value">The enum value.</param>
    /// <returns>The description from the <see cref="DescriptionAttribute"/>, or the enum value's name if no attribute is found.</returns>
    public static string GetDescription(this Enum value)
    {
        var field = value.GetType().GetField(value.ToString());
        if (field == null)
        {
            return value.ToString();
        }

        var attribute = (DescriptionAttribute)Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute))!;
        return attribute.Description;
    }
}