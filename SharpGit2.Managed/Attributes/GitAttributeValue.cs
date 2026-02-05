namespace SharpGit2.Managed.Attributes;

public readonly record struct GitAttributeValue
{
    public readonly ValueType Type;
    public readonly string? String;

    internal GitAttributeValue(ValueType type, string? value = null)
    {
        Type = type;
        String = value;
    }

    public static GitAttributeValue Create(ValueType type, string? value)
    {
        switch (type)
        {
            case ValueType.Unspecified:
            case ValueType.True:
            case ValueType.False:
                if (!string.IsNullOrEmpty(value))
                {
                    throw new ArgumentException($"Invalid attribute value, type {type} doesn't allow a string.");
                }

                break;
            case ValueType.String:
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentException($"Invalid attribute value, type {type} must have a string.");
                }

                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type));
        }

        return new GitAttributeValue(type, value);
    }

    public bool IsUnspecified => Type == ValueType.Unspecified;

    public bool IsTrue => Type == ValueType.True;

    public bool IsFalse => Type == ValueType.False;

    public bool IsString => Type == ValueType.String;

    public enum ValueType
    {
        Unspecified,
        True,
        False,
        String
    }
}
