namespace SharpGit2;

public readonly record struct GitAttributeValue(GitAttributeValueType Type, string? StringValue);