using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.Intrinsics;
using System.Text;

using static SharpGit2.GitNativeApi;

namespace SharpGit2;

/// <summary>
/// Basic type of any Git reference
/// </summary>
[Flags]
public enum GitReferenceType
{
    /// <summary>
    /// Invalid reference
    /// </summary>
    Invalid = 0,
    /// <summary>
    /// A reference that points at an object id
    /// </summary>
    Direct = 1,
    /// <summary>
    /// A reference that points at another reference
    /// </summary>
    Symbolic = 2,
    /// <summary>
    /// Bitmask of all reference types
    /// </summary>
    All = Direct | Symbolic
}

/// <summary>
/// In-memory representation of a reference
/// </summary>
/// <param name="nativeHandle">The native object pointer</param>
[DebuggerDisplay("{NativeHandle == null ? (string?)null : GetName()}")]
public unsafe readonly partial struct GitReference(Git2.Reference* nativeHandle) : IDisposable, IComparable<GitReference>, IGitHandle
{
    /// <summary>
    /// The native object pointer
    /// </summary>
    internal Git2.Reference* NativeHandle { get; } = nativeHandle;

    /// <inheritdoc/>
    public bool IsNull => this.NativeHandle == null;

    public void Dispose()
    {
        git_reference_free(this.NativeHandle);
    }

    public bool IsBranch
    {
        get
        {
            var handle = this.ThrowIfNull();

            var code = git_reference_is_branch(handle.NativeHandle);

            return code != 0;
        }
    }

    public bool IsNote
    {
        get
        {
            var handle = this.ThrowIfNull();

            var code = git_reference_is_note(handle.NativeHandle);

            return code != 0;
        }
    }

    public bool IsRemote
    {
        get
        {
            var handle = this.ThrowIfNull();

            var code = git_reference_is_remote(handle.NativeHandle);

            return code != 0;
        }
    }

    public bool IsTag
    {
        get
        {
            var handle = this.ThrowIfNull();

            var code = git_reference_is_tag(handle.NativeHandle);

            return code != 0;
        }
    }

    public bool IsSymbolic => this.NativeSymbolicTarget != null;

    internal byte* NativeName
    {
        get
        {
            var handle = this.ThrowIfNull();

            return git_reference_name(handle.NativeHandle);
        }
    }

    internal byte* NativeSymbolicTarget
    {
        get
        {
            var handle = this.ThrowIfNull();

            return git_reference_symbolic_target(handle.NativeHandle);
        }
    }

    public GitRepository Owner
    {
        get
        {
            var handle = this.ThrowIfNull();

            return new(git_reference_owner(handle.NativeHandle));
        }
    }

    public GitReferenceType Type
    {
        get
        {
            var handle = this.ThrowIfNull();

            return git_reference_type(handle.NativeHandle);
        }
    }

    public override string ToString()
    {
        return this.NativeHandle == null ? "< NULL HANDLE >" : GetName();
    }

    /// <inheritdoc/>
    public int CompareTo(GitReference other)
    {
        return git_reference_cmp(this.NativeHandle, other.NativeHandle);
    }

    /// <summary>
    /// Deletes the reference from the repository.
    /// </summary>
    /// <remarks>
    /// Does not dispose of the in-memory reference object. Ensure <see cref="Dispose"/> is called after using this method!
    /// </remarks>
    public void Delete()
    {
        var handle = this.ThrowIfNull();

        Git2.ThrowIfError(git_reference_delete(handle.NativeHandle));
    }

    public GitReference Duplicate()
    {
        var handle = this.ThrowIfNull();

        Git2.Reference* reference = null;
        Git2.ThrowIfError(git_reference_dup(&reference, handle.NativeHandle));

        return new(reference);
    }

    public string GetName()
    {
        var handle = this.ThrowIfNull();

        return Git2.GetPooledString(git_reference_name(handle.NativeHandle));
    }

    public bool IsName(ReadOnlySpan<byte> utf8Name)
    {
        var handle = this.ThrowIfNull();

        ReadOnlySpan<byte> name = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(git_reference_name(handle.NativeHandle));

        return name.SequenceEqual(utf8Name);
    }

    public string GetShorthand()
    {
        var handle = this.ThrowIfNull();

        return Git2.GetPooledString(git_reference_shorthand(handle.NativeHandle));
    }

    /// <summary>
    /// Gets the symbolic target of this reference.
    /// </summary>
    /// <returns>The name of the symbolic target, or <see langword="null"/> if this isn't a symbolic reference</returns>
    public string? GetSymbolicTarget()
    {
        byte* target = this.NativeSymbolicTarget;

        if (target is null)
        {
            return null;
        }

        return Git2.GetPooledString(target);
    }

    public GitObjectID? GetTarget()
    {
        var handle = this.ThrowIfNull();

        var ptr = git_reference_target(handle.NativeHandle);

        return ptr is null ? null : *ptr;
    }

    public GitObjectID? GetTargetPeel()
    {
        var handle = this.ThrowIfNull();

        var ptr = git_reference_target_peel(handle.NativeHandle);

        return ptr is null ? null : *ptr;
    }

    public GitObject Peel(GitObjectType type)
    {
        var handle = this.ThrowIfNull();

        if (!Enum.IsDefined(type) || type == GitObjectType.Invalid)
        {
            throw new ArgumentOutOfRangeException(nameof(type));
        }

        Git2.Object* result = null;
        Git2.ThrowIfError(git_reference_peel(&result, handle.NativeHandle, type));

        return new(result);
    }

    public TObject Peel<TObject>() where TObject: struct, IGitHandle, IGitObject<TObject>
    {
        var handle = this.ThrowIfNull();

        Git2.Object* result = null;
        Git2.ThrowIfError(git_reference_peel(&result, handle.NativeHandle, TObject.ObjectType));

        return TObject.FromObjectPointer(result);
    }

    public GitError Peel(GitObjectType type, out GitObject obj)
    {
        var handle = this.ThrowIfNull();

        if (!Enum.IsDefined(type) || type == GitObjectType.Invalid)
        {
            throw new ArgumentOutOfRangeException(nameof(type));
        }

        Git2.Object* result = null;
        var error = git_reference_peel(&result, handle.NativeHandle, type);

        obj = (error == GitError.OK) ? new(result) : default;

        return error;
    }

    /// <summary>
    /// Rename an existing reference.
    /// </summary>
    /// <param name="newName"></param>
    /// <param name="force"></param>
    /// <param name="logMessage"></param>
    /// <returns></returns>
    public GitReference Rename(string newName, bool force, string? logMessage)
    {
        var handle = this.ThrowIfNull();

        Git2.Reference* newReference = null;
        Git2.ThrowIfError(git_reference_rename(&newReference, handle.NativeHandle, newName, force ? 1 : 0, logMessage));

        return new(newReference);
    }

    public GitReference Resolve()
    {
        var handle = this.ThrowIfNull();

        Git2.Reference* peeledReference = null;
        Git2.ThrowIfError(git_reference_resolve(&peeledReference, handle.NativeHandle));

        return new(peeledReference);
    }

    public GitReference SetTarget(GitObjectID Id, string? logMessage)
    {
        var handle = this.ThrowIfNull();

        Git2.Reference* newReference = null;
        Git2.ThrowIfError(git_reference_set_target(&newReference, handle.NativeHandle, &Id, logMessage));

        return new(newReference);
    }

    public GitReference SetTarget(in GitObjectID Id, string? logMessage)
    {
        var handle = this.ThrowIfNull();

        Git2.Reference* newReference = null;
        GitError error;

        fixed (GitObjectID* ptr = &Id)
        {
            error = git_reference_set_target(&newReference, handle.NativeHandle, ptr, logMessage);
        }

        Git2.ThrowIfError(error);

        return new(newReference);
    }

    public GitReference SetSymbolicTarget(string target, string? logMessage)
    {
        var handle = this.ThrowIfNull();

        Git2.Reference* newReference = null;
        Git2.ThrowIfError(git_reference_symbolic_set_target(&newReference, handle.NativeHandle, target, logMessage));

        return new(newReference);
    }

    public GitReference SetSymbolicTarget(GitReference target, string? logMessage)
    {
        var handle = this.ThrowIfNull();

        Git2.Reference* newReference = null;
        Git2.ThrowIfError(git_reference_symbolic_set_target(&newReference, handle.NativeHandle, target.NativeName, logMessage));

        return new(newReference);
    }

    public bool TryGetTarget(out GitObjectID id)
    {
        var handle = this.ThrowIfNull();

        var ptr = git_reference_target(handle.NativeHandle);

        if (ptr is not null)
        {
            id = *ptr;
            return true;
        }

        id = default;
        return false;
    }

    public bool TryGetTargetPeel(out GitObjectID id)
    {
        var handle = this.ThrowIfNull();

        var ptr = git_reference_target_peel(handle.NativeHandle);

        if (ptr != null)
        {
            id = *ptr;
            return true;
        }

        id = default;
        return false;
    }

    public ref readonly GitObjectID DangerousGetTarget()
    {
        var handle = this.ThrowIfNull();

        return ref *git_reference_target(handle.NativeHandle);
    }

    public ref readonly GitObjectID DangerousGetTargetPeel()
    {
        var handle = this.ThrowIfNull();

        return ref *git_reference_target_peel(handle.NativeHandle);
    }

    public static bool IsValidReferenceName(string referenceName)
    {
        return NormalizeReferenceName(null, referenceName, GitReferenceFormat.AllowOneLevel, throwIfInvalid: false);
    }

    public static bool IsValidReferenceName(ReadOnlySpan<char> name)
    {
        return NormalizeReferenceName(null, name, GitReferenceFormat.AllowOneLevel, throwIfInvalid: false);
    }

    private static readonly SearchValues<char> _invalidChars = SearchValues.Create("~^:\\?[");
    private static readonly SearchValues<string> _invalidSequences = SearchValues.Create(["..", "@{"], StringComparison.Ordinal);

    /// <summary>
    /// Managed implementation of <see cref="git_reference_normalize_name(byte*, nuint, byte*, GitReferenceFormat)"/>.
    /// Only allows Ascii.
    /// </summary>
    /// <param name="referenceName"></param>
    /// <param name="format">Flags to constrain name validation rules</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentNullException"/>
    public static string NormalizeReferenceName(string referenceName, GitReferenceFormat format)
    {
        ArgumentException.ThrowIfNullOrEmpty(referenceName);

        var builder = new StringBuilder(Math.Max(referenceName.Length, 32));

        bool success = NormalizeReferenceName(builder, referenceName, format, throwIfInvalid: true);
        Debug.Assert(success);

        return builder.ToString();
    }

    internal static bool NormalizeReferenceName(StringBuilder? builder, ReadOnlySpan<char> referenceName, GitReferenceFormat format, bool throwIfInvalid)
    {
        const string GenericErrorMessage = "Provided reference name is invalid!";

        if (!Ascii.IsValid(referenceName)) // Potentially unnecessary, needs further research
        {
            if (throwIfInvalid)
            {
                ThrowInvalidSpec("Reference name must use only Ascii characters!"); 
            }
            else
            {
                return false;
            }
        }

        referenceName = referenceName.TrimStart('/');

        if (referenceName[^1] == '/')
        {
            if (throwIfInvalid)
            {
                ThrowInvalidSpec("Reference name cannot end with '/'"); 
            }
            else
            {
                return false;
            }
        }

        if (referenceName[^1] == '.')
        {
            if (throwIfInvalid)
            {
                ThrowInvalidSpec("Reference name cannot end with '.'"); 
            }
            else
            {
                return false;
            }
        }

        if (referenceName.ContainsAnyInRange('\0', ' ') || referenceName.ContainsAny(_invalidChars))
        {
            if (throwIfInvalid)
            {
                ThrowInvalidSpec("Reference name has invalid characters!"); 
            }
            else
            {
                return false;
            }
        }

        if (referenceName.ContainsAny(_invalidSequences))
        {
            if (throwIfInvalid)
            {
                ThrowInvalidSpec("Reference name cannot contain the sequences \"..\" or \"@{\"");
            }
            else
            {
                return false;
            }
        }

        int globCount = referenceName.Count('*');

        if ((format & GitReferenceFormat.RefSpecPattern) != 0)
        {
            if (globCount > 1)
            {
                if (throwIfInvalid)
                {
                    ThrowInvalidSpec("Reference name contains more than 1 glob! (A glob being '*')"); 
                }
                else
                {
                    return false;
                }
            }
        }
        else
        {
            if (globCount > 0)
            {
                if (throwIfInvalid)
                {
                    ThrowInvalidSpec("Reference name is not allowed to be a glob pattern! (Not allowed to contain '*')"); 
                }
                else
                {
                    return false;
                }
            }
        }

        int segment_count = referenceName.Count('/') + 1;

        Span<Range> segments = segment_count > 8 ? new Range[segment_count] : stackalloc Range[8];

        segment_count = referenceName.Split(segments, '/', StringSplitOptions.RemoveEmptyEntries);

        if (segment_count == 1)
        {
            if ((format & GitReferenceFormat.AllowOneLevel) == 0)
            {
                if (throwIfInvalid)
                {
                    ThrowInvalidSpec("'One level' reference names are not allowed!"); 
                }
                else
                {
                    return false;
                }
            }

            if ((format & GitReferenceFormat.RefSpecShorthand) == 0
                && !(IsAllCapsAndUnderscore(referenceName) || ((format & GitReferenceFormat.RefSpecPattern) != 0 && referenceName.SequenceEqual("*"))))
            {
                if (throwIfInvalid)
                {
                    // TODO: Figure out what to say here
                    ThrowInvalidSpec(GenericErrorMessage); 
                }
                else
                {
                    return false;
                }
            }
        }
        else // segment_count > 1
        {
            Debug.Assert(segment_count > 1);

            // The first segment is checked
            if (IsAllCapsAndUnderscore(referenceName[segments[0]]))
            {
                if (throwIfInvalid)
                {
                    // TODO: Figure out what to say here
                    ThrowInvalidSpec(GenericErrorMessage); 
                }
                else
                {
                    return false;
                }
            }
        }

        segments = segments[..segment_count];

        for (int i = 0; i < segments.Length; ++i)
        {
            ReadOnlySpan<char> segment = referenceName[segments[i]];

            if (segment.EndsWith(".lock"))
            {
                if (throwIfInvalid)
                {
                    ThrowInvalidSpec("Reference name segment cannot end with \".lock\"");
                }
                else
                {
                    return false;
                }
            }

            if (segment[^1] == '.')
            {
                if (throwIfInvalid)
                {
                    ThrowInvalidSpec("Reference name segment cannot end with '.'"); 
                }
                else
                {
                    return false;
                }
            }

            if (builder is not null)
            {
                if (builder.Length > 0)
                    builder.Append('/');

                builder.Append(segment);
            }
        }

        return true;

        static bool IsAllCapsAndUnderscore(ReadOnlySpan<char> span)
        {
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
                var c = span[i];

                // The original code only expected Ascii with seemingly very limited support for unicode
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
}
