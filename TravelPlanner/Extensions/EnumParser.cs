namespace TravelPlanner.Extensions;

public static class EnumParser<TEnum> where TEnum : struct, Enum
{
    public static bool TryParseDefined(string? text, out TEnum value)
    {
        return Enum.TryParse(text, true, out value) && Enum.IsDefined(value);
    }
}