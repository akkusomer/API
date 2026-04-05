namespace AtlasWeb.Tests.Support;

internal static class ObjectExtensions
{
    public static T? Read<T>(this object source, string propertyName)
    {
        var property = source.GetType().GetProperty(propertyName);
        if (property is null)
        {
            throw new InvalidOperationException($"Property '{propertyName}' was not found on '{source.GetType().Name}'.");
        }

        var value = property.GetValue(source);
        if (value is null)
        {
            return default;
        }

        return (T)value;
    }
}
