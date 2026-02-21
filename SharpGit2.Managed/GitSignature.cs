using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace SharpGit2.Managed;

public readonly partial struct GitSignature : ISpanFormattable, IEquatable<GitSignature>
{
    public string Name { get; }

    public string Email { get; }

    public DateTimeOffset When { get; }

    public GitSignature(string name, string email, DateTimeOffset when = default)
    {
        if (name.ContainsAny('<', '>'))
        {
            ThrowInvalidCharacters(nameof(name));
        }
        
        if (email.ContainsAny('<', '>'))
        {
            ThrowInvalidCharacters(nameof(email));
        }

        Name = name;
        Email = email;
        When = when;

        static void ThrowInvalidCharacters(string paramName)
        {
            throw new ArgumentException("Name or Email contains invalid characters!", paramName);
        }
    }

    public static bool TryParse(ReadOnlySpan<char> value, out GitSignature signature)
    {
        // Regex validationRegex = GetSignatureRegex();
        // if (!validationRegex.IsMatch(value))
        // {
        //     signature = default;
        //     return false;
        // }
        
        value = value.Trim();

        int emailStart = value.IndexOf('<');
        int emailEnd = value.IndexOf('>');

        if (emailStart <= 1 || value[emailStart - 1] != ' '
            || emailEnd < 0
            || emailEnd < emailStart)
        {
            goto Fail;
        }

        var nameSpan = value[..(emailStart - 1)].TrimEnd();
        var emailSpan = value.Slice(emailStart + 1, emailEnd - emailStart - 1).Trim();

        if (nameSpan.IsEmpty || emailSpan.IsEmpty)
            goto Fail;

        DateTimeOffset time = default;

        if (checked((uint)emailEnd + 1) < value.Length)
        {
            if (value[emailEnd + 1] != ' ')
                goto Fail;

            var timeSpan = value[(emailEnd + 2)..];

            int end = timeSpan.IndexOfAnyExceptInRange('0', '9');

            if (end >= 0 && timeSpan[end] != ' ')
                goto Fail;

            if (!ulong.TryParse(end < 0 ? timeSpan : timeSpan[..end], NumberStyles.None, null, out ulong unixTimeSeconds))
                goto Fail;

            time = DateTimeOffset.FromUnixTimeSeconds(checked((long)unixTimeSeconds));

            if (end >= 0)
            {
                if (timeSpan[end] != ' ')
                    goto Fail;
                
                timeSpan = timeSpan[(end + 1)..];

                if (timeSpan.Length != 5)
                    goto Fail;

                if (timeSpan[0] is not '-' and not '+')
                    goto Fail;

                if (!int.TryParse(timeSpan, NumberStyles.AllowLeadingSign, null, out int offset))
                    goto Fail;

                (int hours, int minutes) = Math.DivRem(Math.Abs(offset), 100);

                if (hours > 14 || minutes > 59) // validate the individual values
                    goto Fail;

                var offset2 = hours * 60 + minutes;
                
                if (offset2 > 14 * 60) // validate the combined offset
                    goto Fail;

                time = time.ToOffset(new TimeSpan(0, offset < 0 ? -offset2 : offset2, 0));
            }
        }

        signature = new GitSignature(Utilities.GetPooledString(nameSpan), Utilities.GetPooledString(emailSpan), time);
        return true;
    Fail:
        signature = default;
        return false;
    }

    [GeneratedRegex("^[^<>]+<[^<>]+>( ?[0-9]+( +[+-][0-9]{4})?)?$")]
    private static partial Regex GetSignatureRegex();

    public static GitSignature Now(string name, string email)
    {
        return new GitSignature(name, email, DateTimeOffset.Now);
    }

    public static GitSignature? Default(GitRepository repo)
    {
        var config = repo.GetConfigSnapshot();

        string? name = config?.GetString("user.name");

        if (name == null)
            return null;
        
        string? email = config!.GetString("user.email");

        if (email == null)
            return null;
        
        return Now(name, email);
    }
    
    public override string ToString()
    {
        return ToString(true);
    }

    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        return this.ToString(format == "tc");
    }

    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
    {
        bool includeTimecode = format is "tc";
        
        if (!destination.TryWrite($"{Name} <{Email}>", out int totalWritten))
            goto bufferTooSmall;
        
        if (includeTimecode)
        {
            int offset = When.TotalOffsetMinutes, written;
            Span<char> remainingBuffer = destination.Slice(totalWritten);

            if (offset != 0)
            {
                char sign = offset < 0 ? '-' : '+';
                var (hours, minutes) = Math.DivRem(Math.Abs(offset), 60);

                if (!remainingBuffer.TryWrite($" {When.ToUnixTimeSeconds()} {sign}{hours:00}{minutes:00}", out written))
                    goto bufferTooSmall;
            }
            else
            {
                if (!remainingBuffer.TryWrite($" {When.ToUnixTimeSeconds()}", out written))
                    goto bufferTooSmall;
            }

            totalWritten += written;
        }

        charsWritten = totalWritten;
        return true;
        
    bufferTooSmall:
        charsWritten = 0;
        return false;
    }

    public string ToString(bool includeTimecode)
    {
        if (!this.IsValid)
        {
            return "Invalid Signature";
        }

        scoped Span<char> buffer = default;

        if (includeTimecode)
        {
            buffer = stackalloc char[32];

            int offset = When.TotalOffsetMinutes, written;

            if (offset != 0)
            {
                char sign = offset < 0 ? '-' : '+';
                var (hours, minutes) = Math.DivRem(Math.Abs(offset), 60);

                bool success = buffer.TryWrite($" {When.ToUnixTimeSeconds()} {sign}{hours:00}{minutes:00}", out written);
                Debug.Assert(success);
            }
            else
            {
                bool success = buffer.TryWrite($" {When.ToUnixTimeSeconds()}", out written);
                Debug.Assert(success);
            }

            buffer = buffer.Slice(0, written);
        }

        return $"{Name} <{Email}>{buffer}";
    }

    public bool IsValid => !string.IsNullOrWhiteSpace(this.Name) && !string.IsNullOrWhiteSpace(this.Email);

    public bool Equals(GitSignature other)
    {
        return this.Name == other.Name && this.Email == other.Email && this.When.Equals(other.When);
    }

    public override bool Equals(object? obj)
    {
        return obj is GitSignature other && this.Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(this.Name, this.Email, this.When);
    }
}
