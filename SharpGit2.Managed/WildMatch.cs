namespace SharpGit2.Managed;

internal static class WildMatch
{
    [Flags]
    public enum Flags
    {
        CaseFold = 1,
        PathName = 2,
    }

    public enum Result
    {
        Match = 0,
        NoMatch = 1,
        AbortAll = -1,
        AbortToStarStar = -2
    }

    private enum SpecialCharacterType
    {
        Space = 0x01,
        Digit = 0x02,
        Alpha = 0x04,
        GlobSpecial = 0x08,
        RegexSpecial = 0x10,
        PathspecMagic = 0x20,
        Control = 0x40,
        Punctuation = 0x80
    }

    private const byte S = (byte)SpecialCharacterType.Space,
        A = (byte)SpecialCharacterType.Alpha,
        D = (byte)SpecialCharacterType.Digit,
        G = (byte)SpecialCharacterType.GlobSpecial,
        R = (byte)SpecialCharacterType.RegexSpecial,
        P = (byte)SpecialCharacterType.PathspecMagic,
        X = (byte)SpecialCharacterType.Control,
        U = (byte)SpecialCharacterType.Punctuation,
        Z = (byte)(SpecialCharacterType.Control | SpecialCharacterType.Space);

    private static readonly byte[] sane_ctype = new byte[128]
    {
        X, X, X, X, X, X, X, X, X, Z, Z, X, X, Z, X, X,		/*   0.. 15 */
	    X, X, X, X, X, X, X, X, X, X, X, X, X, X, X, X,		/*  16.. 31 */
	    S, P, P, P, R, P, P, P, R, R, G, R, P, P, R, P,		/*  32.. 47 */
	    D, D, D, D, D, D, D, D, D, D, P, P, P, P, P, G,		/*  48.. 63 */
	    P, A, A, A, A, A, A, A, A, A, A, A, A, A, A, A,		/*  64.. 79 */
	    A, A, A, A, A, A, A, A, A, A, A, G, G, U, R, P,		/*  80.. 95 */
	    P, A, A, A, A, A, A, A, A, A, A, A, A, A, A, A,		/*  96..111 */
	    A, A, A, A, A, A, A, A, A, A, A, R, R, U, P, X,		/* 112..127 */
    };

    private const char NegateClass1 = '!';
    private const char NegateClass2 = '^';

    private static bool IsGlobSpecial(char c)
    {
        uint character = c;

        return character < (uint)sane_ctype.Length && (sane_ctype[character] & (int)SpecialCharacterType.GlobSpecial) != 0;
    }

    private static bool IsAsciiPunct(char c)
    {
        return char.IsAscii(c) && char.IsPunctuation(c);
    }

    private static bool IsAsciiGraph(char c)
    {
        return char.IsAsciiLetterOrDigit(c)
            || IsAsciiPunct(c);
    }

    // TODO: Support unicode matching with `System.Text.Rune`
    // While it's not necessarily supported by other implementations, I think it would be nice here.
    public static Result Match(ReadOnlySpan<char> pattern, ReadOnlySpan<char> text, Flags flags)
    {
        int pattern_pos = 0, text_pos = 0;
        for (; (uint)pattern_pos < (uint)pattern.Length; ++pattern_pos, ++text_pos)
        {
            char p_ch = pattern[pattern_pos];
            char t_ch;

            if ((uint)text_pos < (uint)text.Length)
            {
                t_ch = text[text_pos];
            }
            else if (p_ch == '*')
            {
                t_ch = '\0';
            }
            else
                return Result.AbortAll;

            if ((flags & Flags.CaseFold) != 0)
            {
                if (char.IsAsciiLetterUpper(p_ch)) // unicode characters will remain untouched
                    p_ch = char.ToLower(p_ch);
                
                if (char.IsAsciiLetterUpper(t_ch))
                    t_ch = char.ToLower(t_ch);
            }

            bool match_slash = false;
            switch (p_ch)
            {
                case '\\':
                    p_ch = (uint)++pattern_pos >= (uint)pattern.Length ? '\0' : pattern[pattern_pos];
                    goto default;

                case '?':
                    if ((flags & Flags.PathName) != 0 && t_ch == '/')
                    {
                        return Result.NoMatch;
                    }

                    continue;
                case '*':
                    if ((uint)++pattern_pos < (uint)pattern.Length && pattern[pattern_pos] == '*')
                    {
                        int prev_pos = pattern_pos - 2;

                        while ((uint)pattern_pos < (uint)pattern.Length && pattern[pattern_pos] == '*')
                        {
                            pattern_pos += 1;
                        }

                        if ((flags & Flags.PathName) == 0)
                        {
                            match_slash = true;
                        }
                        else if ((prev_pos < 0 || pattern[prev_pos] == '/')
                            && ((uint)pattern_pos >= (uint)pattern.Length || pattern[pattern_pos] == '/' || pattern.Slice(pattern_pos).StartsWith("\\/")))
                        {
                            /*
					         * Assuming we already match 'foo/' and are at
					         * <star star slash>, just assume it matches
					         * nothing and go ahead match the rest of the
					         * pattern with the remaining string. This
					         * helps make foo/<*><*>/bar (<> because
					         * otherwise it breaks C comment syntax) match
					         * both foo/bar and foo/a/bar.
					         */
                            if ((uint)pattern_pos < (uint)pattern.Length && pattern[pattern_pos] == '/' && Match(pattern.Slice(pattern_pos + 1), text.Slice(text_pos), flags) == Result.Match)
                                return Result.Match;

                            match_slash = true;
                        }
                        else
                        {
                            match_slash = false;
                        }
                    }
                    else
                    {
                        match_slash = (flags & Flags.PathName) == 0;
                    }

                    if ((uint)pattern_pos >= (uint)pattern.Length)
                    {
                        if (!match_slash && text.Slice(text_pos).TrimEnd('/').Contains('/'))
                            return Result.NoMatch;

                        return Result.Match;
                    }
                    else if (!match_slash && pattern[pattern_pos] == '/')
                    {
                        /*
				         * _one_ asterisk followed by a slash
				         * with WM_PATHNAME matches the next
				         * directory
				         */
                        int pos = text.Slice(text_pos).IndexOf('/');

                        if (pos < 0)
                            return Result.NoMatch;

                        text_pos += pos;
                        break;
                    }
                    
                    while (true)
                    {
                        if ((uint)text_pos >= (uint)text.Length)
                            break;

                        if (!IsGlobSpecial(pattern[pattern_pos]))
                        {
                            p_ch = pattern[pattern_pos];

                            if ((flags & Flags.CaseFold) != 0 && char.IsAsciiLetterUpper(p_ch))
                                p_ch = char.ToLower(p_ch);

                            while ((uint)text_pos < (uint)text.Length && (match_slash || text[text_pos] != '/'))
                            {
                                t_ch = text[text_pos];

                                if ((flags & Flags.CaseFold) != 0 && char.IsAsciiLetterUpper(t_ch))
                                    t_ch = char.ToLower(t_ch);

                                if (p_ch == t_ch)
                                    break;

                                text_pos += 1;
                            }

                            if (p_ch != t_ch)
                                return Result.NoMatch;
                        }

                        var matched = Match(pattern.Slice(pattern_pos), text.Slice(text_pos), flags);
                        if (matched != Result.NoMatch)
                        {
                            if (!match_slash || matched != Result.AbortToStarStar)
                            {
                                return matched;
                            }
                        }
                        else if (!match_slash && t_ch == '/')
                        {
                            return Result.AbortToStarStar;
                        }

                        text_pos += 1;
                    }

                    return Result.AbortAll;

                case '[':
                {
                    pattern_pos += 1;

                    if ((uint)pattern_pos >= (uint)pattern.Length)
                        return Result.AbortAll;

                    bool negated = pattern[pattern_pos] is NegateClass1 or NegateClass2;

                    if (negated)
                    {
                        pattern_pos += 1;

                        if ((uint)pattern_pos >= (uint)pattern.Length)
                            return Result.AbortAll;
                    }

                    char prev_ch = '\0';
                    bool matched = false;

                    // This statement was added to conform to tests found online
                    if (pattern[pattern_pos] == ']') // empty character class
                    {
                        // Rewind by one character and continue matching the rest of the pattern.
                        // It is debatable whether empty character classes should be allowed like this.
                        text_pos -= 1;
                        continue;
                    }

                    do
                    {
                        p_ch = pattern[pattern_pos];

                        if (p_ch == '\\')
                        {
                            if ((uint)++pattern_pos >= (uint)pattern.Length)
                                return Result.AbortAll;

                            p_ch = pattern[pattern_pos];

                            matched |= t_ch == p_ch;
                        }
                        else if (p_ch == '-' && prev_ch != 0 && (uint)pattern_pos + 1 < (uint)pattern.Length && pattern[pattern_pos + 1] != ']')
                        {
                            p_ch = pattern[++pattern_pos];

                            if (p_ch == '\\')
                            {
                                if ((uint)++pattern_pos >= (uint)pattern.Length)
                                    return Result.AbortAll;

                                p_ch = pattern[pattern_pos];
                            }

                            if (t_ch <= p_ch && t_ch >= prev_ch)
                                matched = true;
                            else if ((flags & Flags.CaseFold) != 0 && char.IsAsciiLetterLower(t_ch))
                            {
                                var tmp = char.ToUpper(t_ch);

                                matched |= tmp <= p_ch & tmp >= prev_ch;
                            }

                            p_ch = '\0';
                        }
                        else if (p_ch == '[' && (uint)pattern_pos + 1 < (uint)pattern.Length && pattern[pattern_pos + 1] == ':')
                        {
                            int s = pattern_pos += 2;

                            for (; (uint)pattern_pos < (uint)pattern.Length && pattern[pattern_pos] != ']'; ++pattern_pos)
                            {
                            }

                            if ((uint)pattern_pos >= (uint)pattern.Length)
                                return Result.AbortAll;

                            if (pattern_pos - 2 < s || pattern[pattern_pos - 1] != ':')
                            {
                                pattern_pos = s - 2;
                                p_ch = '[';

                                matched |= t_ch == p_ch;

                                goto loop_continue;
                            }

                            ReadOnlySpan<char> className = pattern[s..(pattern_pos - 1)];

                            switch (className.Length)
                            {
                                case 5:
                                    switch (className[0])
                                    {
                                        case 'a' or 'A':
                                            if (className.Equals("alnum", StringComparison.OrdinalIgnoreCase))
                                            {
                                                matched |= char.IsAsciiLetterOrDigit(t_ch);
                                                break;
                                            }
                                            else if (className.Equals("alpha", StringComparison.OrdinalIgnoreCase))
                                            {
                                                matched |= char.IsAsciiLetter(t_ch);
                                                break;
                                            }

                                            goto default;

                                        case 'b' or 'B':
                                            if (className.Equals("blank", StringComparison.OrdinalIgnoreCase))
                                            {
                                                matched |= t_ch == ' ' | t_ch == '\t';
                                                break;
                                            }

                                            goto default;

                                        case 'c' or 'C':
                                            if (className.Equals("cntrl", StringComparison.OrdinalIgnoreCase))
                                            {
                                                matched |= char.IsBetween(t_ch, '\0', '\x1f') | t_ch == '\x7f';
                                                break;
                                            }

                                            goto default;

                                        case 'd' or 'D':
                                            if (className.Equals("digit", StringComparison.OrdinalIgnoreCase))
                                            {
                                                matched |= char.IsAsciiDigit(t_ch);
                                                break;
                                            }

                                            goto default;

                                        case 'g' or 'G':
                                            if (className.Equals("graph", StringComparison.OrdinalIgnoreCase))
                                            {
                                                matched |= IsAsciiGraph(t_ch);
                                                break;
                                            }

                                            goto default;

                                        case 'l' or 'L':
                                            if (className.Equals("lower", StringComparison.OrdinalIgnoreCase))
                                            {
                                                matched |= (flags & Flags.CaseFold) != 0 ? char.IsAsciiLetter(t_ch) : char.IsAsciiLetterLower(t_ch);
                                                break;
                                            }

                                            goto default;

                                        case 'p' or 'P':
                                            if (className.Equals("print", StringComparison.OrdinalIgnoreCase))
                                            {
                                                matched |= t_ch == ' ' || IsAsciiGraph(t_ch);
                                                break;
                                            }
                                            else if (className.Equals("punct", StringComparison.OrdinalIgnoreCase))
                                            {
                                                matched |= IsAsciiPunct(t_ch);
                                                break;
                                            }

                                            goto default;

                                        case 's' or 'S':
                                            if (className.Equals("space", StringComparison.OrdinalIgnoreCase))
                                            {
                                                if (char.IsAscii(t_ch))
                                                {
                                                    matched |= char.IsWhiteSpace(t_ch);
                                                }

                                                break;
                                            }

                                            goto default;

                                        case 'u' or 'U':
                                            if (className.Equals("upper", StringComparison.OrdinalIgnoreCase))
                                            {
                                                matched |= (flags & Flags.CaseFold) != 0 ? char.IsAsciiLetter(t_ch) : char.IsAsciiLetterUpper(t_ch);
                                                break;
                                            }

                                            goto default;

                                        default:
                                            return Result.AbortAll;
                                    }

                                    break;
                                case 6:
                                    if (className.Equals("xdigit", StringComparison.OrdinalIgnoreCase))
                                    {
                                        matched |= char.IsAsciiHexDigit(t_ch);
                                        break;
                                    }

                                    goto default;
                                default:
                                    return Result.AbortAll;
                            }

                            //switch (className)
                            //{
                            //    case "alnum":
                            //        matched |= char.IsAsciiLetterOrDigit(t_ch);
                            //        break;

                            //    case "alpha":
                            //        matched |= char.IsAsciiLetter(t_ch);
                            //        break;

                            //    case "blank":
                            //        matched |= t_ch == ' ' | t_ch == '\t';
                            //        break;

                            //    case "cntrl":
                            //        matched |= char.IsBetween(t_ch, '\0', '\x1f') | t_ch == '\x7f';
                            //        break;

                            //    case "digit":
                            //        matched |= char.IsAsciiDigit(t_ch);
                            //        break;

                            //    case "graph":
                            //        matched |= IsAsciiGraph(t_ch);
                            //        break;

                            //    case "lower":
                            //        matched |= (flags & Flags.CaseFold) != 0 ? char.IsAsciiLetter(t_ch) : char.IsAsciiLetterLower(t_ch);
                            //        break;

                            //    case "print":
                            //        matched |= t_ch == ' ' || IsAsciiGraph(t_ch);
                            //        break;

                            //    case "punct":
                            //        matched |= IsAsciiPunct(t_ch);
                            //        break;

                            //    case "space":
                            //        if (char.IsAscii(t_ch))
                            //        {
                            //            matched |= char.IsWhiteSpace(t_ch);
                            //        }

                            //        break;

                            //    case "upper":
                            //        matched |= (flags & Flags.CaseFold) != 0 ? char.IsAsciiLetter(t_ch) : char.IsAsciiLetterUpper(t_ch);
                            //        break;

                            //    case "xdigit":
                            //        matched |= char.IsAsciiHexDigit(t_ch);
                            //        break;

                            //    default: // malformed [:class:] string
                            //        return Result.AbortAll;
                            //}

                            p_ch = '\0';
                        }
                        else
                        {
                            matched |= t_ch == p_ch;
                        }

                    loop_continue:
                        prev_ch = p_ch;
                        pattern_pos += 1;
                    }
                    while ((uint)pattern_pos < (uint)pattern.Length && pattern[pattern_pos] != ']');

                    if ((uint)pattern_pos >= (uint)pattern.Length || matched == negated || ((flags & Flags.PathName) != 0 && t_ch == '/'))
                        return Result.NoMatch;

                    continue;
                }

                default:
                    if (p_ch != t_ch)
                    {
                        return Result.NoMatch;
                    }

                    continue;
            }
        }

        return (uint)text_pos < (uint)text.Length ? Result.NoMatch : Result.Match;
    }
}
