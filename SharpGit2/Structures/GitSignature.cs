using System.Diagnostics;
using System.Globalization;

namespace SharpGit2
{
    public unsafe readonly struct GitSignature
    {
        public readonly string Name;
        public readonly string Email;
        public readonly DateTimeOffset When;

        /// <summary>
        /// Constructs a signature from name, email, and time
        /// </summary>
        /// <param name="name"></param>
        /// <param name="email"></param>
        /// <param name="when"></param>
        /// <exception cref="ArgumentException"></exception>
        /// <remarks>
        /// Matches behavior with <see cref="GitNativeApi.git_signature_new(Native.GitSignature**, string, string, ulong, int)"/>
        /// </remarks>
        public GitSignature(string name, string email, DateTimeOffset when)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(email);

            if (name.AsSpan().ContainsAny('<', '>')
                || email.AsSpan().ContainsAny('<', '>'))
            {
                throw new ArgumentException("Name or Email contains angle brackets!");
            }

            Name = name.Trim();
            Email = email.Trim();
            When = when;
        }

        public GitSignature(in Native.GitSignature sig)
        {
            ArgumentNullException.ThrowIfNull(sig.Name);
            if (*sig.Name == 0)
            {
                throw new ArgumentException("Native string is empty!", "sig.Name");
            }

            ArgumentNullException.ThrowIfNull(sig.Email);
            if (*sig.Email == 0)
            {
                throw new ArgumentException("Native string is empty!", "sig.Email");
            }

            Name = Git2.GetPooledString(sig.Name)!;
            Email = Git2.GetPooledString(sig.Email)!;
            When = (DateTimeOffset)sig.When;
        }

        public bool IsValid => !string.IsNullOrWhiteSpace(Name) && !string.IsNullOrWhiteSpace(Email);

        public override string ToString()
        {
            return ToString(true);
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

        public static GitSignature Now(string name, string email)
        {
            return new(name, email, DateTimeOffset.Now);
        }

        public static bool TryParse(ReadOnlySpan<char> value, out GitSignature signature)
        {
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

            if (emailEnd + 1 < value.Length)
            {
                if (value[emailEnd + 1] != ' ')
                    goto Fail;

                var timeSpan = value[(emailEnd + 2)..];

                int end = timeSpan.IndexOfAnyExceptInRange('0', '9');

                if (end >= 0 && timeSpan[end] != ' ')
                    goto Fail;

                if (!ulong.TryParse(end < 0 ? timeSpan : timeSpan[..end], NumberStyles.None, null, out ulong unixTimeSeconds))
                    goto Fail;

                time = DateTimeOffset.FromUnixTimeSeconds((long)unixTimeSeconds);

                if (end >= 0)
                {
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

                    offset = int.CopySign(hours * 60 + minutes, offset);

                    if (Math.Abs(offset) > 14 * 60) // validate the combined offset
                        goto Fail;

                    time = time.ToOffset(new TimeSpan(0, offset, 0));
                }
            }

            signature = new(nameSpan.ToString(), emailSpan.ToString(), time);
            return true;
        Fail:
            signature = default;
            return false;
        }
    }
}

namespace SharpGit2.Native
{
    public unsafe readonly struct GitSignature
    {
        public readonly byte* Name;
        public readonly byte* Email;
        public readonly GitTime When;

        public override string ToString()
        {
            if (this.Name is null || *this.Name == 0
                || this.Email is null || *this.Email == 0) // Null or empty
            {
                return "Invalid Signature";
            }

            return new SharpGit2.GitSignature(in this).ToString();
        }
    }
}
