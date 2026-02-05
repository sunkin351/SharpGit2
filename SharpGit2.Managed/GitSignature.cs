using System.Text.RegularExpressions;
using CommunityToolkit.HighPerformance.Buffers;

namespace SharpGit2.Managed;

public readonly partial struct GitSignature
{
    public string Name { get; }

    public string Email { get; }

    public DateTimeOffset When { get; }

    public GitSignature(string name, string email, DateTimeOffset when = default)
    {
        if (name.ContainsAny('<', '>') || email.ContainsAny('<', '>'))
        {
            throw new ArgumentException("Name or Email contains invalid characters!");
        }

        Name = name;
        Email = email;
        When = when;
    }

    public static bool TryParse(ReadOnlySpan<char> text, out GitSignature signature)
    {
        Regex validationRegex = GetSignatureRegex();
        if (!validationRegex.IsMatch(text))
        {
            signature = default;
            return false;
        }

        int firstSep = text.IndexOf('<');

        ReadOnlySpan<char> name = text[..firstSep].Trim();

        int secondSep = text.Slice(firstSep + 1).IndexOf('>') + firstSep + 1;

        ReadOnlySpan<char> email = text[(firstSep + 1)..secondSep].Trim();

        Utilities.GetPooledString(name);

        throw new NotImplementedException();
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
}
