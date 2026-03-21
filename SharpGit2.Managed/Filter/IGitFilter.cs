using System.Collections.Immutable;
using SharpGit2.Managed.Attributes;

namespace SharpGit2.Managed.Filter;

public interface IGitFilter : IDisposable
{
    /// <summary>
    /// A whitespace delimited list of attribute names to check for this filter (e.g. "eol crlf text").
    /// If the attribute name is null or empty, it will be simply loaded and <see cref="ShouldFilter"/>
    /// will be called. If it has a value (i.e. "name=value"), the attribute must match that value for the filter to be applied.
    /// The value may be a wildcard (e.g. "value=*"), in which case the filter will be invoked for any value for the given attribute name.
    /// See the attribute parameter of the <see cref="ShouldFilter"/> callback
    /// for the attribute value that was specified.
    /// </summary>
    string? Attributes { get; }
    
    /// <summary>
    /// This callback will be invoked right before the first use of the filter,
    /// so you can defer expensive initialization operations.
    /// </summary>
    void Initialize();

    bool ShouldFilter(
        GitFilterSource source,
        IReadOnlyDictionary<string, GitAttributeValue> attributes,
        ref object? context);

    Stream WriteStream(GitFilterSource source, Stream next, ref object? context);
}