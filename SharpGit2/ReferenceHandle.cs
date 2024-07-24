﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading.Tasks;

namespace SharpGit2;

[Flags]
public enum GitReferenceType
{
    Invalid = 0,
    Direct = 1,
    Symbolic = 2,
    All = Direct | Symbolic
}

public unsafe readonly partial struct ReferenceHandle(nint handle) : IDisposable, IComparable<ReferenceHandle>
{
    internal readonly nint NativeHandle = handle;

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

    public RepositoryHandle Owner => new(NativeApi.git_reference_owner(NativeHandle));

    public GitReferenceType TargetType => NativeApi.git_reference_target_type(NativeHandle);

    public void Dispose()
    {
        NativeApi.git_reference_free(NativeHandle);
    }

    public int CompareTo(ReferenceHandle other)
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

    public ReferenceHandle Duplicate()
    {
        ReferenceHandle reference;
        Git2.ThrowIfError(NativeApi.git_reference_dup(&reference, NativeHandle));

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

    public GitObjectHandle Peel(GitObjectType type)
    {
        GitObjectHandle result;
        Git2.ThrowIfError(NativeApi.git_reference_peel(&result, NativeHandle, type));

        return result;
    }

    public GitError Peel(GitObjectType type, out GitObjectHandle obj)
    {
        GitObjectHandle result;
        var error = NativeApi.git_reference_peel(&result, NativeHandle, type);

        obj = (error == GitError.OK) ? result : default;

        return error;
    }

    public ReferenceHandle Rename(string newName, bool force, string? logMessage)
    {
        ReferenceHandle newReference;
        Git2.ThrowIfError(NativeApi.git_reference_rename(&newReference, NativeHandle, newName, force ? 1 : 0, logMessage));

        return newReference;
    }

    public ReferenceHandle Resolve()
    {
        ReferenceHandle peeledReference;
        Git2.ThrowIfError(NativeApi.git_reference_resolve(&peeledReference, NativeHandle));

        return peeledReference;
    }

    public ReferenceHandle SetTarget(GitObjectID Id, string? logMessage)
    {
        ReferenceHandle newReference;
        Git2.ThrowIfError(NativeApi.git_reference_set_target(&newReference, NativeHandle, &Id, logMessage));

        return newReference;
    }

    public ReferenceHandle SetTarget(in GitObjectID Id, string? logMessage)
    {
        ReferenceHandle newReference;
        GitError error;

        fixed (GitObjectID* ptr = &Id)
        {
            error = NativeApi.git_reference_set_target(&newReference, NativeHandle, ptr, logMessage);
        }

        Git2.ThrowIfError(error);

        return newReference;
    }

    public ReferenceHandle SetSymbolicTarget(string target, string? logMessage)
    {
        ReferenceHandle newReference;
        Git2.ThrowIfError(NativeApi.git_reference_symbolic_set_target(&newReference, NativeHandle, target, logMessage));

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
}
