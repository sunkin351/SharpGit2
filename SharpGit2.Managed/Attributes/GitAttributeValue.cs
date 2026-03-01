using System.Diagnostics;

namespace SharpGit2.Managed.Attributes;

[DebuggerDisplay("{this.DebuggerDisplay()}")]
public readonly record struct GitAttributeValue
{
    public readonly ValueType Type;
    public readonly string? String;

    internal GitAttributeValue(ValueType type, string? value = null)
    {
        Type = type;
        String = value;
    }
    
    public bool IsUnspecified => Type == ValueType.Unspecified;

    public bool IsTrue => Type == ValueType.True;

    public bool IsFalse => Type == ValueType.False;

    public bool IsString => Type == ValueType.String;

    public static GitAttributeValue Unspecified => new(ValueType.Unspecified, null);
    public static GitAttributeValue True => new(ValueType.True, null);
    public static GitAttributeValue False => new(ValueType.False, null);
    
    public static implicit operator GitAttributeValue(string? value)
    {
        return new GitAttributeValue(value == null ? ValueType.Unspecified : ValueType.String, value);
    }

    public static implicit operator GitAttributeValue(bool value)
    {
        return new GitAttributeValue(value ? ValueType.True : ValueType.False, null);
    }

    public enum ValueType
    {
        Unspecified,
        True,
        False,
        String
    }

    private string DebuggerDisplay()
    {
        return this.Type == ValueType.String ? $"\"{this.String!}\"" : this.Type.ToString();
    }
}
