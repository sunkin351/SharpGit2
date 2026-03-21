using System.Buffers;
using System.Diagnostics;

using CommunityToolkit.HighPerformance.Buffers;

using SharpGit2.Managed.Attributes;

namespace SharpGit2.Managed.Filter;

public sealed class GitIdentFilter : IGitFilter
{
    public string? Attributes => "+ident";
    
    public void Initialize()
    {
    }

    public void Dispose()
    {
    }

    public bool ShouldFilter(GitFilterSource source, IReadOnlyDictionary<string, GitAttributeValue> attributes, ref object? context)
    {
        return true;
    }

    public Stream WriteStream(GitFilterSource source, Stream next, ref object? context)
    {
        Debug.Assert(next.CanWrite);

        if (source is { Mode: GitFilterMode.ToWorktree, Oid.IsZero: true })
            return next; // pass through
        
        return new GitFilterBufferedStream(source, next, (input, output) =>
        {
            if (Utilities.IsBinaryData(input))
            {
                return false;
            }

            return source.Mode == GitFilterMode.ToWorktree ? InsertId(input, output, source) : RemoveId(input, output);
        });
    }

    private static bool InsertId(ReadOnlySpan<byte> input, IBufferWriter<byte> output, GitFilterSource source)
    {
        Debug.Assert(!source.Oid.IsZero);

        var range = FindId(input);

        if (range is null)
            return false;

        var (offset, length) = range.GetValueOrDefault().GetOffsetAndLength(input.Length);

        output.Write(input[..offset]);
        
        ReadOnlySpan<byte> prefix = "$Id: "u8;
        ReadOnlySpan<byte> postfix = " $"u8;

        Span<byte> valueBuffer = output.GetSpan(prefix.Length + postfix.Length + GitObjectID.MaxHexSize);

        prefix.CopyTo(valueBuffer);
        
        bool success = source.Oid.TryFormat(valueBuffer[prefix.Length..], out int written);
        Debug.Assert(success);
        
        postfix.CopyTo(valueBuffer[(prefix.Length + written)..]);
        
        output.Advance(prefix.Length + written + postfix.Length); 
        
        output.Write(input.Slice(offset + length));
        return true;
    }

    private static bool RemoveId(ReadOnlySpan<byte> input, IBufferWriter<byte> output)
    {
        var range = FindId(input);

        if (range is null)
            return false;

        var (offset, length) = range.GetValueOrDefault().GetOffsetAndLength(input.Length);

        var replacement = "$Id$"u8;
        
        if (output is ArrayPoolBufferWriter<byte> or ArrayBufferWriter<byte>)
        {
            // Special code path to avoid needing to do a bunch of needless copying under the hood,
            // if the underlying buffer writer hasn't allocated up to the necessary array size.
            int lengthToWrite = input.Length - length + replacement.Length;
            var span = output.GetSpan(lengthToWrite);
            
            input[..offset].CopyTo(span);
            replacement.CopyTo(span[offset..]);
            input[(offset + length)..].CopyTo(span[(offset + replacement.Length)..]);
            
            Debug.Assert(offset + replacement.Length + (input.Length - (offset + length)) == lengthToWrite);
            
            output.Advance(lengthToWrite);
        }
        else
        {
            output.Write(input[..offset]);
            output.Write(replacement);
            output.Write(input[(offset + length)..]);
        }
        
        return true;
    }

    private static Range? FindId(ReadOnlySpan<byte> input)
    {
        int idx = input.IndexOf("$Id"u8);
        if (idx >= 0)
        {
            var tmp = input.Slice(idx + 3);

            int end;
            if (tmp.StartsWith((byte)'$'))
                end = idx + 4;
            else
            {
                int idx2 = tmp.IndexOf((byte)'$');

                if (idx2 < 0)
                {
                    return null;
                }

                end = idx2 + idx + 4;
            }

            return idx..end;
        }

        return null;
    }
}