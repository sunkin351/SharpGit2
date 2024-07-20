using System;
using System.Collections.Generic;
using System.Linq;
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

public unsafe readonly partial struct ReferenceHandle(nint handle) : IDisposable
{
    internal readonly nint NativeHandle = handle;

    public bool IsBranch
    {
        get
        {
            var code = git_reference_is_branch(NativeHandle);

            return Git2.ErrorOrBoolean(code);
        }
    }

    public bool IsNote
    {
        get
        {
            var code = git_reference_is_note(NativeHandle);

            return Git2.ErrorOrBoolean(code);
        }
    }

    public bool IsRemote
    {
        get
        {
            var code = git_reference_is_remote(NativeHandle);

            return Git2.ErrorOrBoolean(code);
        }
    }

    public bool IsTag
    {
        get
        {
            var code = git_reference_is_tag(NativeHandle);

            return Git2.ErrorOrBoolean(code);
        }
    }

    public void Dispose()
    {
        git_reference_free(NativeHandle);
    }

    public void Delete()
    {
        Git2.ThrowIfError(git_reference_delete(NativeHandle));
    }

    public string GetName()
    {
        return Utf8StringMarshaller.ConvertToManaged(git_reference_name(NativeHandle))!;
    }

    public ReferenceHandle Duplicate()
    {
        ReferenceHandle reference;
        Git2.ThrowIfError(git_reference_dup(&reference, NativeHandle));

        return reference;
    }
}
