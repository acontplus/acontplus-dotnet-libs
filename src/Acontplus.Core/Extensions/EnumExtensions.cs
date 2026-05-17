namespace Acontplus.Core.Extensions;

/// <summary>Extension methods for <see cref="Enum"/> values.</summary>
public static class EnumExtensions
{
    /// <summary>
    /// Returns the <see cref="DescriptionAttribute"/> text for an enum value,
    /// or the enum member name when no description is defined.
    /// </summary>
    public static string DisplayName(this Enum value)
    {
        var type = value.GetType();

        var memInfo = type.GetMember(value.ToString());

        switch (memInfo.Length)
        {
            case > 0:
                {
                    var attrs = memInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);
                    if (attrs.Length > 0)
                    {
                        return ((DescriptionAttribute)attrs[0]).Description;
                    }

                    break;
                }
        }

        return value.ToString();
    }
}
