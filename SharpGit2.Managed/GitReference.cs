using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Text;

using CommunityToolkit.HighPerformance.Buffers;
using JetBrains.Annotations;
using SharpGit2.Managed.Config;
using SharpGit2.Managed.ReferenceDB;

namespace SharpGit2.Managed;

public enum GitReferenceType
{
    Invalid,
    Direct,
    Symbolic
}

[Flags, PublicAPI]
public enum GitReferenceFormat
{
    Normal = 0,
    AllowOneLevel = 1,
    RefSpecPattern = 1 << 1,
    RefSpecShorthand = 1 << 2,
}

[PublicAPI]
public sealed class GitReference : IComparable<GitReference>
{
    internal const GitReferenceFormat PrecomposeUnicodeFlag = (GitReferenceFormat)(1 << 16);
    internal const GitReferenceFormat ValidationDisableFlag = (GitReferenceFormat)(1 << 15);

    public GitRepository Owner => this.ThrowIfDatabaseIsNull().Repository;

    internal GitReferenceDatabase? DB { get; set; }
    public string ReferenceName { get; }

    public GitReferenceType ReferenceType { get; }
    
    public string? SymbolicTarget { get; }

    private readonly GitObjectID _directTarget;
    internal readonly GitObjectID _peel;
    
    public ref readonly GitObjectID  DirectTarget => ref _directTarget;

    public bool IsBranch => ReferenceNameIsBranch(this.ReferenceName);

    public bool IsRemote => ReferenceNameIsRemote(this.ReferenceName);

    public bool IsTag => ReferenceNameIsTag(this.ReferenceName);

    public bool IsNote => ReferenceNameIsNote(this.ReferenceName);

    public bool IsUnbornHead => this.ReferenceType != GitReferenceType.Direct
                                && this.ReferenceName == Constants.GitHeadFile
                                && this.Owner.References.LookupResolved(this.SymbolicTarget!) == null;

    internal GitReference(string referenceName, GitObjectID target, GitObjectID? peel, GitReferenceDatabase? db = null)
    {
        Debug.Assert(!string.IsNullOrWhiteSpace(referenceName));
        
        this.ReferenceName = referenceName;
        this.ReferenceType = GitReferenceType.Direct;
        _directTarget = target;
        _peel = peel.GetValueOrDefault();
        this.DB = db;
    }

    internal GitReference(string referenceName, string symbolicTarget, GitReferenceDatabase? db = null)
    {
        Debug.Assert(!string.IsNullOrWhiteSpace(referenceName));
        Debug.Assert(!string.IsNullOrWhiteSpace(symbolicTarget));
        
        this.ReferenceName = referenceName;
        this.ReferenceType = GitReferenceType.Symbolic;
        this.SymbolicTarget = symbolicTarget;
        this.DB = db;
    }
    
    public int CompareTo(GitReference? other)
    {
        ArgumentNullException.ThrowIfNull(other);

        int cmp = this.ReferenceName.CompareTo(other.ReferenceName, StringComparison.InvariantCulture);

        if (cmp != 0)
            return cmp;

        var type1 = this.ReferenceType;
        var type2 = other.ReferenceType;

        Debug.Assert(type1 is GitReferenceType.Symbolic or GitReferenceType.Direct
            && type2 is GitReferenceType.Symbolic or GitReferenceType.Direct);
        
        if (type1 != type2)
            return type1 == GitReferenceType.Symbolic ? -1 : 1;

        return type1 == GitReferenceType.Symbolic
            ? this.SymbolicTarget.CompareTo(other.SymbolicTarget, StringComparison.InvariantCulture)
            : this.DirectTarget.CompareTo(other.DirectTarget);
    }

    internal GitReference WithReferenceName(string referenceName)
    {
        return this.ReferenceType switch
        {
            GitReferenceType.Direct => new GitReference(referenceName, _directTarget, _peel, this.DB),
            GitReferenceType.Symbolic => new GitReference(referenceName, this.SymbolicTarget!, this.DB),
            _ => throw new InvalidOperationException("Invalid reference type!")
        };
    }

    public void Delete()
    {
        if (this.ReferenceName == "HEAD")
            throw new InvalidOperationException("Cannot delete HEAD!");

        var database = this.DB
            ?? throw new InvalidOperationException("Reference was not initialized properly! (Not associated with a reference database)");

        if (this.ReferenceType == GitReferenceType.Direct)
        {
            database.Delete(this.ReferenceName, in this.DirectTarget);
        }
        else
        {
            Debug.Assert(this.ReferenceType == GitReferenceType.Symbolic);
            database.Delete(this.ReferenceName, this.SymbolicTarget!);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="target"></param>
    /// <param name="logMessage"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public GitReference SetTarget(in GitObjectID target, string? logMessage = null)
    {
        if (this.ReferenceType != GitReferenceType.Direct)
            throw new InvalidOperationException("Cannot set OID on a symbolic reference!");

        var database = this.DB ??
                       throw new InvalidOperationException(
                           "Reference was not initialized properly! (Not associated with a reference database)");
        
        return database.Repository.References.CreateMatching(
            this.ReferenceName,
            in target,
            true,
            in _directTarget,
            logMessage);
    }
    
    public GitReference SetTarget(string symbolicTarget, string? logMessage = null)
    {
        if (this.ReferenceType != GitReferenceType.Symbolic)
            throw new InvalidOperationException("Cannot set symbolic target on a direct reference!");

        var database = this.DB ??
                       throw new InvalidOperationException(
                           "Reference was not initialized properly! (Not associated with a reference database)");
        
        return this.DB.Repository.References.CreateMatching(
            this.ReferenceName,
            symbolicTarget,
            true,
            this.SymbolicTarget!,
            logMessage);
    }

    public GitReference Rename(string newName, bool force, string? logMessage = null)
    {
        var database = this.ThrowIfDatabaseIsNull();

        var repo = database.Repository;

        var signature = LogSignature(repo);

        newName = ReferenceNormalizeForRepo(repo, newName, true);

        string oldName = this.ReferenceName;
        var newRef = database.Rename(oldName, newName, force, signature, logMessage);

        foreach (var worktree in repo.EnumerateWorktreesInternal())
        {
            var head = worktree.References.Lookup(Constants.GitHeadFile);

            if (head?.ReferenceType != GitReferenceType.Symbolic || head.SymbolicTarget != oldName)
                continue;

            head.SetTarget(newName, null);
        }

        return newRef;
    }

    public GitReference Resolve()
    {
        return this.ReferenceType switch
        {
            GitReferenceType.Direct => this, // Git Reference objects should be immutable
            GitReferenceType.Symbolic => this.Owner.References.LookupResolved(this.SymbolicTarget!)!,
            _ => throw new InvalidOperationException("Invalid Reference!")
        };
    }

    public GitObject Peel(GitObjectType targetType = GitObjectType.Any)
    {
        GitReference resolved = this.ReferenceType == GitReferenceType.Direct ? this : this.Resolve();

        GitObject? target;
        /*
         * If we try to peel an object to a tag, we cannot use
         * the fully peeled object, as that will always resolve
         * to a commit. So we only want to use the peeled value
         * if it is not zero and the target is not a tag.
         */
        try
        {
            if (targetType != GitObjectType.Tag && !_peel.IsZero)
            {
                target = this.Owner.LookupObject(in _peel);
            }
            else
            {
                target = this.Owner.LookupObject(in _directTarget);
            }
        }
        catch (Exception e)
        {
            throw new Git2Exception("Cannot retrieve reference target!", e);
        }

        if (targetType == GitObjectType.Any && target is not { ObjectType: GitObjectType.Tag })
        {
            return target;
        }
        else
        {
            return target.Peel(targetType);
        }

        throw new NotImplementedException();

        static void ThrowPeelError(GitReference reference, string message)
        {
            throw new InvalidOperationException(
                $"The reference '{reference.ReferenceName}' cannot be peeled - {message}");
        }
    }

    private GitReferenceDatabase ThrowIfDatabaseIsNull()
    {
        return this.DB ?? throw new InvalidOperationException(
            "Reference was not initialized properly! (Not associated with a reference database, this is a bug inside SharpGit2)");
    }
    
    internal static GitSignature LogSignature(GitRepository repo)
    {
        if (repo is { IdentityName: { Length: > 0 } name, IdentityEmail: { Length: > 0 } email })
            return GitSignature.Now(name, email);

        if (GitSignature.Default(repo) is GitSignature signature)
            return signature;

        return GitSignature.Now("unknown", "unknown");
    }

    public static bool IsReferenceNameValid(string referenceName)
    {
        return IsReferenceNameValid((ReadOnlySpan<char>)referenceName);
    }

    public static bool IsReferenceNameValid(ReadOnlySpan<char> referenceName)
    {
        return IsReferenceNameValid(referenceName, GitReferenceFormat.AllowOneLevel);
    }

    internal static bool IsReferenceNameValid(ReadOnlySpan<char> referenceName, GitReferenceFormat flags)
    {
        return NormalizeReferenceName(null, referenceName, flags, false);
    }
    
    public static string NormalizeReferenceName(string referenceName, GitReferenceFormat format)
    {
        ArgumentException.ThrowIfNullOrEmpty(referenceName);

        using var builder = new ArrayPoolBufferWriter<char>(Math.Max(256, referenceName.Length));

        bool success = NormalizeReferenceName(builder, referenceName, format, throwIfInvalid: true);
        Debug.Assert(success);

        var written = builder.WrittenSpan;
        
        return written.SequenceEqual(referenceName) ? referenceName : Utilities.GetPooledString(written);
    }

    private static readonly SearchValues<char> _invalidChars = SearchValues.Create(
        "\0\u0001\u0002\u0003\u0004\u0005\u0006\a\b\t\n\v\f\r\u000e\u000f\u0010\u0011\u0012\u0013\u0014\u0015\u0016\u0017\u0018\u0019\u001a\e\u001c\u001d\u001e\u001f ~^:\\?[");
    private static readonly SearchValues<string> _invalidSequences = SearchValues.Create(["..", "@{"], StringComparison.Ordinal);

    internal static bool NormalizeReferenceName(IBufferWriter<char>? buffer, ReadOnlySpan<char> referenceName, GitReferenceFormat format, bool throwIfInvalid)
    {
        const string GenericErrorMessage = "Provided reference name is invalid!";

        if (referenceName.IsEmpty)
        {
            if (throwIfInvalid)
                ThrowInvalidSpec(GenericErrorMessage);

            return false;
        }

        char[]? poolArray = null;
        try
        {
            if ((format & PrecomposeUnicodeFlag) != 0)
            {
                poolArray = ArrayPool<char>.Shared.Rent(Math.Max(64, referenceName.Length));
                int written;
                
                // Loop until the buffer is big enough (only expect 0, 1, or maybe 2 iterations at most)
                while (!referenceName.TryNormalize(poolArray, out written, NormalizationForm.FormC))
                {
                    char[] tmp = ArrayPool<char>.Shared.Rent(poolArray.Length + 1);
                    ArrayPool<char>.Shared.Return(poolArray);
                    poolArray = tmp;
                }

                referenceName = poolArray.AsSpan(0, written);
            }
            
            if ((format & ValidationDisableFlag) != 0)
            {
                buffer?.Write(referenceName);
                return true;
            }

            switch (referenceName)
            {
                case ['/', ..]:
                {
                    if (throwIfInvalid)
                        ThrowInvalidSpec("Reference name cannot start with '/'");

                    return false;
                }
                case [.., '/']:
                {
                    if (throwIfInvalid)
                        ThrowInvalidSpec("Reference name cannot end with '/'"); 
                
                    return false;
                }
                case [.., '.']:
                {
                    if (throwIfInvalid)
                        ThrowInvalidSpec("Reference name cannot end with '.'"); 
                
                    return false;
                }
            }

            // allow caret prefix to signify a negative refspec.
            if ((referenceName.StartsWith('^') ? referenceName[1..] : referenceName).ContainsAny(_invalidChars))
            {
                if (throwIfInvalid)
                    ThrowInvalidSpec("Reference name has invalid characters!");
                
                return false;
            }

            if (referenceName.ContainsAny(_invalidSequences))
            {
                if (throwIfInvalid)
                    ThrowInvalidSpec("Reference name cannot contain the sequences \"..\" or \"@{\"");
                
                return false;
            }

            int globCount = referenceName.Count('*');

            if ((format & GitReferenceFormat.RefSpecPattern) != 0)
            {
                if (globCount > 1)
                {
                    if (throwIfInvalid)
                        ThrowInvalidSpec("Reference name contains more than 1 glob! (A glob being '*')"); 
                    
                    return false;
                }
            }
            else
            {
                if (globCount > 0)
                {
                    if (throwIfInvalid)
                        ThrowInvalidSpec("Reference name is not allowed to be a glob pattern! (Not allowed to contain '*')");
                    
                    return false;
                }
            }

            int segmentCount = referenceName.Count('/') + 1;

            Span<Range> segments = segmentCount > 8 ? new Range[segmentCount] : stackalloc Range[8];

            segmentCount = referenceName.Split(segments, '/', StringSplitOptions.RemoveEmptyEntries);

            if (segmentCount == 1)
            {
                if ((format & GitReferenceFormat.AllowOneLevel) == 0)
                {
                    if (throwIfInvalid)
                        ThrowInvalidSpec("'One level' reference names are not allowed!");
                    
                    return false;
                }

                if ((format & GitReferenceFormat.RefSpecShorthand) == 0
                    && !(IsAllCapsAndUnderscore(referenceName) || ((format & GitReferenceFormat.RefSpecPattern) != 0 && referenceName.SequenceEqual("*"))))
                {
                    if (throwIfInvalid)
                    {
                        // TODO: Figure out what to say here
                        ThrowInvalidSpec(GenericErrorMessage); 
                    }
                    
                    return false;
                }
            }
            else // segment_count > 1
            {
                Debug.Assert(segmentCount > 1);

                // The first segment is checked
                if (IsAllCapsAndUnderscore(referenceName[segments[0]]))
                {
                    if (throwIfInvalid)
                    {
                        // TODO: Figure out what to say here
                        ThrowInvalidSpec(GenericErrorMessage); 
                    }
                    
                    return false;
                }
            }

            segments = segments[..segmentCount];
            bool writtenToBuilder = false;

            foreach (var range in segments)
            {
                ReadOnlySpan<char> segment = referenceName[range];

                if (segment.EndsWith(".lock", StringComparison.OrdinalIgnoreCase))
                {
                    if (throwIfInvalid)
                        ThrowInvalidSpec("Reference name segment cannot end with \".lock\"");
                    
                    return false;
                }

                if (segment[^1] == '.')
                {
                    if (throwIfInvalid)
                        ThrowInvalidSpec("Reference name segment cannot end with '.'");
                    
                    return false;
                }

                if (buffer is not null)
                {
                    Span<char> span;
                    int written;
                    
                    if (writtenToBuilder)
                    {
                        span = buffer.GetSpan(1 + segment.Length);

                        span[0] = '/';
                        segment.CopyTo(span[1..]);
                        
                        buffer.Advance(1 + segment.Length);
                    }
                    else
                    {
                        span = buffer.GetSpan(segment.Length);
                        
                        segment.CopyTo(span);
                        
                        buffer.Advance(segment.Length);
                        
                        writtenToBuilder = true;
                    }
                }
            }
        }
        finally
        {
            if (poolArray != null)
                ArrayPool<char>.Shared.Return(poolArray);
        }
        
        return true;

        static bool IsAllCapsAndUnderscore(ReadOnlySpan<char> span)
        {
            Debug.Assert(span.Length > 0);
            
            if (span[0] == '_' || span[^1] == '_')
                return false;

            // Vectorized code
            if (Vector128.IsHardwareAccelerated && span.Length >= Vector128<ushort>.Count)
            {
                ref readonly ushort data = ref Unsafe.As<char, ushort>(ref MemoryMarshal.GetReference(span));
                int lenMinusOneVec = span.Length - Vector128<ushort>.Count;

                var min = Vector128.Create((ushort)'A');
                var max = Vector128.Create((ushort)('Z' - 'A'));
                var underscore = Vector128.Create((ushort)'_');
                Vector128<ushort> value, cmp;

                int i = 0;
                while (i < lenMinusOneVec)
                {
                    value = Vector128.LoadUnsafe(in data, (nuint)i);
                    
                    cmp = Vector128.LessThanOrEqual(value - min, max);
                    cmp |= Vector128.Equals(value, underscore);

                    if (Vector128.EqualsAny(cmp, Vector128<ushort>.Zero))
                        return false;

                    i += Vector128<ushort>.Count;
                }

                value = Vector128.LoadUnsafe(in data, (nuint)lenMinusOneVec);

                cmp = Vector128.LessThanOrEqual(value - min, max);
                cmp |= Vector128.Equals(value, underscore);

                if (Vector128.EqualsAny(cmp, Vector128<ushort>.Zero))
                    return false;

                return true;
            }

            // Scalar code
            for (int i = 0; i < span.Length; ++i)
            {
                char c = span[i];

                // The original code only expected Ascii with seemingly very limited support for Unicode
                if (!char.IsAsciiLetterUpper(c) && c != '_')
                {
                    return false;
                }
            }

            return true;
        }

        [DoesNotReturn]
        static void ThrowInvalidSpec(string message)
        {
            throw new ArgumentException(message, nameof(referenceName));
        }
    }

    internal static void ThrowIfInvalidReferenceName(ReadOnlySpan<char> referenceName, GitReferenceFormat format)
    {
        bool success = NormalizeReferenceName(null, referenceName, format, throwIfInvalid: true);
        Debug.Assert(success);
    }

    internal static string GetShorthand(string referenceName)
    {
        throw new NotImplementedException();
    }

    internal static bool ReferenceNameIsBranch(ReadOnlySpan<char> referenceName)
    {
        return referenceName.StartsWith(Constants.RefsHeadsDir);
    }

    internal static bool ReferenceNameIsRemote(ReadOnlySpan<char> referenceName)
    {
        return referenceName.StartsWith(Constants.RefsRemotesDir);
    }

    internal static bool ReferenceNameIsTag(ReadOnlySpan<char> referenceName)
    {
        return referenceName.StartsWith(Constants.RefsTagsDir);
    }
    
    internal static bool ReferenceNameIsNote(string referenceName)
    {
        return referenceName.StartsWith(Constants.RefsNotesDir);
    }
    
            
    internal static string ReferenceNormalizeForRepo(GitRepository repo, string name, bool validate)
    {
        GitReferenceFormat flags = GitReferenceFormat.AllowOneLevel;

        if (repo.ConfigMapLookup(GitConfigMapItem.Precompose) != 0)
            flags |= GitReference.PrecomposeUnicodeFlag;

        if (!validate)
            flags |= GitReference.ValidationDisableFlag;

        return NormalizeReferenceName(name, flags);
    }

}
