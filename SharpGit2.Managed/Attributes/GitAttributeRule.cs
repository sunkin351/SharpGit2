using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;

using CommunityToolkit.HighPerformance.Buffers;

namespace SharpGit2.Managed.Attributes;

internal struct GitAttributeRule
{
    public GitAttributeFNMatch Match;
    public Dictionary<string, string> Assigns;

    public GitAttributeValue GetValueForAssign(string assignName)
    {
        if (Assigns.TryGetValue(assignName, out var value))
        {
            Debug.Assert(value != null);

            if (ReferenceEquals(value, Constants.attribute_internal_true))
                return GitAttributeValue.True;

            if (ReferenceEquals(value, Constants.attribute_internal_false))
                return GitAttributeValue.False;
            
            if (ReferenceEquals(value, Constants.attribute_internal_unset))
                return GitAttributeValue.Unspecified;
            
            return (GitAttributeValue)value;
        }

        return GitAttributeValue.Unspecified;
    }

    private static readonly StringPool _attributeNamePool = new StringPool(256);

    internal static bool TryParse(
        ReadOnlySpan<char> input,
        GitAttributeFNMatchFlags flags,
        string? context,
        GitRepository? repo,
        out int consumed,
        out GitAttributeRule rule)
    {
        rule = default;

        flags &= GitAttributeFNMatchFlags._IncomingMask;

        bool allow_space = (flags & GitAttributeFNMatchFlags.AllowSpace) != 0;
        int currentPos = 0;

        // Parse FNMatch pattern
        if (!allow_space)
        {
            while (currentPos < input.Length)
            {
                if (!char.IsWhiteSpace(input[currentPos]))
                    break;

                currentPos += 1;
            }
        }

        if (currentPos == input.Length)
        {
            consumed = currentPos;
            return false;
        }

        if (input[currentPos] == '#')
        {
            int idx = input.Slice(currentPos).IndexOf('\n');

            consumed = idx < 0 ? input.Length : idx + currentPos + 1;
            return false;
        }

        if (input[currentPos] == '\n')
        {
            consumed = currentPos + 1;
            return false;
        }

        if (input.Slice(currentPos).StartsWith("\r\n"))
        {
            consumed = currentPos + 2;
            return false;
        }

        if ((flags & GitAttributeFNMatchFlags.AllowMacro) != 0 && input.StartsWith("[attr]"))
        {
            currentPos += 6;
            rule.Match.Flags |= GitAttributeFNMatchFlags.Macro;
        }

        if ((flags & GitAttributeFNMatchFlags.AllowNegation) != 0
            && (uint)currentPos < (uint)input.Length
            && input[currentPos] == '!')
        {
            currentPos += 1;
            rule.Match.Flags |= GitAttributeFNMatchFlags.Negate;
        }

        int slash_count = 0, pattern_start = currentPos;
        bool escaped = false;

        for (; (uint)currentPos < (uint)input.Length; ++currentPos)
        {
            var c = input[currentPos];

            if (c == '\\' && !escaped)
            {
                escaped = true;
                continue;
            }

            if (!escaped && char.IsWhiteSpace(c))
            {
                if (!allow_space || (c is not ' ' and not '\t' and not '\r'))
                    break;
            }
            else if (c == '/')
            {
                rule.Match.Flags |= GitAttributeFNMatchFlags.FullPath;
                slash_count += 1;

                if (slash_count == 1 && currentPos == pattern_start)
                {
                    pattern_start = currentPos + 1;
                }
            }
            else if (!escaped && c is '*' or '?' or '[')
            {
                rule.Match.Flags |= GitAttributeFNMatchFlags.HasWild;
            }

            escaped = false;
        }

        if (currentPos == input.Length)
        {
            consumed = currentPos;
            return false;
        }

        Debug.Assert((uint)currentPos < (uint)input.Length);

        int pattern_end = currentPos - 1;

        while (pattern_end >= 0 && char.IsWhiteSpace(input[pattern_end]))
        {
            pattern_end -= 1;
        }

        if (pattern_end >= 0 && input[pattern_end] == '\\' && (uint)pattern_end + 1 < (uint)input.Length)
        {
            pattern_end += 1;
        }

        var pattern = input[pattern_start..(pattern_end + 1)];

        if (pattern.EndsWith('/'))
        {
            pattern = pattern[..^1];
            rule.Match.Flags |= GitAttributeFNMatchFlags.Directory;

            if (--slash_count <= 0)
            {
                rule.Match.Flags &= ~GitAttributeFNMatchFlags.FullPath;
            }
        }

        if (pattern.IsEmpty)
        {
            consumed = currentPos;
            return false;
        }

        if (context is not null)
        {
            int idx = context.LastIndexOf('/');

            if (idx >= 0)
                rule.Match.ContainingDir = StringPool.Shared.GetOrAdd(context.AsSpan(0, idx + 1)); // include the slash for easier matching
        }

        rule.Match.Pattern = UnescapeSpaceAndMakeString(pattern);

        static string UnescapeSpaceAndMakeString(ReadOnlySpan<char> pattern)
        {
            if (!pattern.Contains('\\'))
                return pattern.ToString();

            char[] buffer = ArrayPool<char>.Shared.Rent(pattern.Length);
            int pos = 0;

            for (int index = 0; index < pattern.Length; ++index)
            {
                var c = pattern[index];

                if (c == '\\' && index + 1 < pattern.Length && char.IsWhiteSpace(pattern[index + 1]))
                {
                    continue;
                }

                buffer[pos++] = c;
            }

            var result = new string(buffer, 0, pos);

            ArrayPool<char>.Shared.Return(buffer);

            return result;
        }

        // Parse attributes associated with pattern
        Dictionary<string, string> assignments = new();

        while ((uint)currentPos < (uint)input.Length && input[currentPos] != '\n')
        {
            bool shouldBreak = false;
            while ((uint)currentPos < (uint)input.Length && char.IsWhiteSpace(input[currentPos]))
            {
                if (input[currentPos] == '\n')
                {
                    shouldBreak = true;
                    break;
                }

                currentPos += 1;
            }

            if (shouldBreak || (uint)currentPos >= (uint)input.Length)
                break;

            string value = Constants.attribute_internal_true;

            if (input[currentPos] == '-')
            {
                value = Constants.attribute_internal_false;
                currentPos += 1;
            }
            else if (input[currentPos] == '!')
            {
                value = Constants.attribute_internal_unset;
                currentPos += 1;
            }
            else if (input[currentPos] == '#')
            {
                int nlPos = input.Slice(currentPos).IndexOf('\n');

                currentPos = nlPos < 0 ? input.Length : currentPos + nlPos + 1;

                break;
            }

            int begin = currentPos;

            while ((uint)currentPos < (uint)input.Length
                && !char.IsWhiteSpace(input[currentPos])
                && input[currentPos] != '=')
            {
                currentPos += 1;
            }

            var name = _attributeNamePool.GetOrAdd(input[begin..currentPos]);

            if ((uint)currentPos < (uint)input.Length && input[currentPos] == '=')
            {
                begin = ++currentPos;

                for (; (uint)currentPos < (uint)input.Length && !char.IsWhiteSpace(input[currentPos]); ++currentPos)
                {
                }

                if (currentPos != begin)
                {
                    value = input[begin..currentPos].ToString();
                }
            }

            if (repo != null && value == Constants.attribute_internal_true)
            {
                throw new NotImplementedException();
            }

            assignments[name] = value;
        }

        consumed = currentPos + ((uint)currentPos < (uint)input.Length && input[currentPos] == '\n' ? 1 : 0);
        rule.Assigns = assignments;
        return assignments.Count > 0;
    }

    internal static bool TryParseAssignments(ReadOnlySpan<char> input, GitRepository repo, ref GitAttributeRule rule)
    {
        Dictionary<string, string> assignments = new();
        int currentPos = 0;

        while ((uint)currentPos < (uint)input.Length && input[currentPos] != '\n')
        {
            bool shouldBreak = false;
            while ((uint)currentPos < (uint)input.Length && char.IsWhiteSpace(input[currentPos]))
            {
                if (input[currentPos] == '\n')
                {
                    shouldBreak = true;
                    break;
                }

                currentPos += 1;
            }

            if (shouldBreak || (uint)currentPos >= (uint)input.Length)
                break;

            string value = Constants.attribute_internal_true;

            if (input[currentPos] == '-')
            {
                value = Constants.attribute_internal_false;
                currentPos += 1;
            }
            else if (input[currentPos] == '!')
            {
                value = Constants.attribute_internal_unset;
                currentPos += 1;
            }
            else if (input[currentPos] == '#')
            {
                int nlPos = input.Slice(currentPos).IndexOf('\n');

                currentPos = nlPos < 0 ? input.Length : currentPos + nlPos + 1;

                break;
            }

            int begin = currentPos;

            while ((uint)currentPos < (uint)input.Length
                && !char.IsWhiteSpace(input[currentPos])
                && input[currentPos] != '=')
            {
                currentPos += 1;
            }

            var name = _attributeNamePool.GetOrAdd(input[begin..currentPos]);

            if ((uint)currentPos < (uint)input.Length && input[currentPos] == '=')
            {
                begin = ++currentPos;

                for (; (uint)currentPos < (uint)input.Length && !char.IsWhiteSpace(input[currentPos]); ++currentPos)
                {
                }

                if (currentPos != begin)
                {
                    value = input[begin..currentPos].ToString();
                }
            }

            if (repo != null && value == Constants.attribute_internal_true)
            {
                throw new NotImplementedException();
            }

            assignments[name] = value;
        }

        rule.Assigns = assignments;
        return assignments.Count > 0;
    }

    internal bool IsMatch(in GitAttributePath path)
    {
        bool result = this.Match.IsMatch(in path);

        Debug.Assert(Unsafe.BitCast<bool, byte>(result) is 0 or 1, "Non-standard boolean value returned from GitAttributeFNMatch.IsMatch()!");

        // negate if the negate flag is set, branchless
        return result ^ ((this.Match.Flags & GitAttributeFNMatchFlags.Negate) != 0);
    }
}
