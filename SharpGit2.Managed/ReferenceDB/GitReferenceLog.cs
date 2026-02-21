using System.Diagnostics.CodeAnalysis;

namespace SharpGit2.Managed.ReferenceDB;

internal struct GitReferenceLogEntry
{
    internal GitObjectID ObjectID_Old;
    internal GitObjectID ObjectID_Current;
    internal GitSignature Committer;
    internal string? Message;
}

public sealed class GitReferenceLog
{
    internal const string RefLogDir = "logs/";
    internal const UnixFileMode DirMode = (UnixFileMode)0x1ff; /*0777*/
    internal const UnixFileMode FileMode = (UnixFileMode)0x1B6; /*0666*/

    internal GitReferenceDatabase DB;
    
    public required string ReferenceName
    {
        get;
        set
        {
            GitReference.ThrowIfInvalidReferenceName(value, GitReferenceFormat.Normal);
            field = value;
        }
    }

    internal GitObjectIDType ObjectIdType;
    internal readonly List<GitReferenceLogEntry> Entries = new();

    public GitReferenceLog()
    {
    }

    [SetsRequiredMembers]
    internal GitReferenceLog(string referenceName, GitObjectIDType objectIdType)
    {
        this.ReferenceName = referenceName;
        ObjectIdType = objectIdType;
    }

    internal GitReferenceLog Duplicate()
    {
        var newObj = new GitReferenceLog()
        {
            ReferenceName = this.ReferenceName
        };

        newObj.Entries.AddRange(this.Entries);

        return newObj;
    }
}
