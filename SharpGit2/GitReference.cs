using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.Intrinsics;
using System.Text;

namespace SharpGit2;

[Flags]
public enum GitReferenceType
{
    Invalid = 0,
    Direct = 1,
    Symbolic = 2,
    All = Direct | Symbolic
}

public unsafe readonly partial struct GitReference : IDisposable, IComparable<GitReference>
{
    internal readonly Git2.Reference* NativeHandle;

    internal GitReference(Git2.Reference* handle)
    {
        NativeHandle = handle;
    }

    public bool IsBranch
    {
        get
        {
            var code = NativeApi.git_reference_is_branch(NativeHandle);

            return code != 0;
        }
    }

    public bool IsNote
    {
        get
        {
            var code = NativeApi.git_reference_is_note(NativeHandle);

            return code != 0;
        }
    }

    public bool IsRemote
    {
        get
        {
            var code = NativeApi.git_reference_is_remote(NativeHandle);

            return code != 0;
        }
    }

    public bool IsTag
    {
        get
        {
            var code = NativeApi.git_reference_is_tag(NativeHandle);

            return code != 0;
        }
    }

    internal byte* NativeName => NativeApi.git_reference_name(NativeHandle);

    internal byte* NativeShorthand => NativeApi.git_reference_shorthand(NativeHandle);

    internal byte* NativeSymbolicTarget => NativeApi.git_reference_symbolic_target(NativeHandle);

    public GitRepository Owner => new(NativeApi.git_reference_owner(NativeHandle));

    public GitReferenceType TargetType => NativeApi.git_reference_target_type(NativeHandle);

    public void Dispose()
    {
        NativeApi.git_reference_free(NativeHandle);
    }

    public override string ToString()
    {
        return NativeHandle == null ? "< NULL >" : GetName();
    }

    public int CompareTo(GitReference other)
    {
        return NativeApi.git_reference_cmp(NativeHandle, other.NativeHandle);
    }

    /// <summary>
    /// Deletes the reference from the repository.
    /// </summary>
    /// <remarks>
    /// Does not dispose of the in-memory reference object. Ensure <see cref="Dispose"/> is called after using this method!
    /// </remarks>
    public void Delete()
    {
        Git2.ThrowIfError(NativeApi.git_reference_delete(NativeHandle));
    }

    public GitReference Duplicate()
    {
        GitReference reference;
        Git2.ThrowIfError(NativeApi.git_reference_dup((Git2.Reference**)&reference, NativeHandle));

        return reference;
    }

    public string GetName()
    {
        return Utf8StringMarshaller.ConvertToManaged(this.NativeName)!;
    }

    public string GetShorthand()
    {
        return Utf8StringMarshaller.ConvertToManaged(this.NativeShorthand)!;
    }

    /// <summary>
    /// Gets the symbolic target of this reference.
    /// </summary>
    /// <returns>The name of the symbolic target, or <see langword="null"/> if this isn't a symbolic reference</returns>
    public string? GetSymbolicTarget()
    {
        return Utf8StringMarshaller.ConvertToManaged(this.NativeSymbolicTarget);
    }

    public GitObject Peel(GitObjectType type)
    {
        GitObject result;
        Git2.ThrowIfError(NativeApi.git_reference_peel(&result, NativeHandle, type));

        return result;
    }

    public GitError Peel(GitObjectType type, out GitObject obj)
    {
        GitObject result;
        var error = NativeApi.git_reference_peel(&result, NativeHandle, type);

        obj = (error == GitError.OK) ? result : default;

        return error;
    }

    public GitReference Rename(string newName, bool force, string? logMessage)
    {
        GitReference newReference;
        Git2.ThrowIfError(NativeApi.git_reference_rename((Git2.Reference**)&newReference, NativeHandle, newName, force ? 1 : 0, logMessage));

        return newReference;
    }

    public GitReference Resolve()
    {
        GitReference peeledReference;
        Git2.ThrowIfError(NativeApi.git_reference_resolve((Git2.Reference**)&peeledReference, NativeHandle));

        return peeledReference;
    }

    public GitReference SetTarget(GitObjectID Id, string? logMessage)
    {
        GitReference newReference;
        Git2.ThrowIfError(NativeApi.git_reference_set_target((Git2.Reference**)&newReference, NativeHandle, &Id, logMessage));

        return newReference;
    }

    public GitReference SetTarget(in GitObjectID Id, string? logMessage)
    {
        GitReference newReference;
        GitError error;

        fixed (GitObjectID* ptr = &Id)
        {
            error = NativeApi.git_reference_set_target((Git2.Reference**)&newReference, NativeHandle, ptr, logMessage);
        }

        Git2.ThrowIfError(error);

        return newReference;
    }

    public GitReference SetSymbolicTarget(string target, string? logMessage)
    {
        GitReference newReference;
        Git2.ThrowIfError(NativeApi.git_reference_symbolic_set_target((Git2.Reference**)&newReference, NativeHandle, target, logMessage));

        return newReference;
    }

    public GitReference SetSymbolicTarget(GitReference target, string? logMessage)
    {
        GitReference newReference;
        Git2.ThrowIfError(NativeApi.git_reference_symbolic_set_target((Git2.Reference**)&newReference, NativeHandle, target.NativeName, logMessage));

        return newReference;
    }

    public bool TryGetTarget(out GitObjectID id)
    {
        var ptr = NativeApi.git_reference_target(NativeHandle);

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
        var ptr = NativeApi.git_reference_target_peel(NativeHandle);

        if (ptr is not null)
        {
            id = *ptr;
            return true;
        }

        id = default;
        return false;
    }

    public static bool IsValidReferenceName(string referenceName)
    {
        int code = NativeApi.git_reference_is_valid_name(referenceName);

        return code != 0;
    }

    private static readonly SearchValues<char> _invalidChars = SearchValues.Create("~^:\\?[");

    /// <summary>
    /// Managed implementation of <see cref="NativeApi.git_reference_normalize_name(byte*, nuint, byte*, GitReferenceFormat)"/>.
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

        const string GenericErrorMessage = "Provided reference name is invalid!";

        ReadOnlySpan<char> source = referenceName;

        if (!Ascii.IsValid(source)) // Potentially unnecessary, needs further research
            ThrowInvalidSpec("Reference name must use only Ascii characters!");

        source = source.TrimStart('/');

        if (source[^1] == '/')
            ThrowInvalidSpec("Reference name cannot end with '/'");

        if (source[^1] == '.')
            ThrowInvalidSpec("Reference name cannot end with '.'");

        if (source.IndexOfAny(_invalidChars) >= 0 || source.IndexOfAnyInRange('\0', ' ') >= 0)
            ThrowInvalidSpec("Reference name has invalid characters!");

        int globCount = source.Count('*');

        if ((format & GitReferenceFormat.RefSpecPattern) != 0)
        {
            if (globCount > 1)
            {
                ThrowInvalidSpec("Reference name contains more than 1 glob! (A glob being '*')");
            }
        }
        else
        {
            if (globCount > 0)
            {
                ThrowInvalidSpec("Reference name is not allowed to be a glob pattern! (Not allowed to contain '*')");
            }
        }

        if (source.Contains("..", StringComparison.Ordinal)
            || source.Contains("@{", StringComparison.Ordinal))
        {
            ThrowInvalidSpec("Reference name cannot contain the sequences \"..\" or \"@{\"");
        }

        int segment_count = source.Count('/') + 1;

        Span<Range> segments = segment_count > 16 ? new Range[segment_count] : stackalloc Range[segment_count];

        segment_count = source.Split(segments, '/', StringSplitOptions.RemoveEmptyEntries);

        if (segment_count == 1)
        {
            if ((format & GitReferenceFormat.AllowOneLevel) == 0)
                ThrowInvalidSpec("'One level' reference names are not allowed!");

            if ((format & GitReferenceFormat.RefSpecShorthand) == 0
                && !(IsAllCapsAndUnderscore(source) || ((format & GitReferenceFormat.RefSpecPattern) != 0 && source.SequenceEqual("*"))))
            {
                // TODO: Figure out what to say here
                ThrowInvalidSpec(GenericErrorMessage);
            }
        }
        else // segment_count > 1
        {
            Debug.Assert(segment_count > 1);

            // The first segment is checked
            if (IsAllCapsAndUnderscore(source[segments[0]]))
            {
                // TODO: Figure out what to say here
                ThrowInvalidSpec(GenericErrorMessage);
            }
        }

        segments = segments[..segment_count];

        var builder = new StringBuilder();

        for (int i = 0; i < segments.Length; ++i)
        {
            ReadOnlySpan<char> segment = source[segments[i]];

            if (segment.EndsWith(".lock"))
            {
                ThrowInvalidSpec("Reference name segment cannot end with \".lock\"");
            }

            if (segment[^1] == '.')
            {
                ThrowInvalidSpec("Reference name segment cannot end with '.'");
            }

            if (builder.Length > 0)
                builder.Append('/');

            builder.Append(segment);
        }

        return builder.ToString();

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
}
