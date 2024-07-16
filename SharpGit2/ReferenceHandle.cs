using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading.Tasks;

namespace SharpGit2;

internal unsafe readonly partial struct ReferenceHandle(nint handle) : IDisposable
{
    internal readonly nint Handle = handle;

    public bool IsBranch
    {
        get
        {
            var code = git_reference_is_branch(Handle);

            return Git2.ErrorOrBoolean(code);
        }
    }

    public bool IsNote
    {
        get
        {
            var code = git_reference_is_note(Handle);

            return Git2.ErrorOrBoolean(code);
        }
    }

    public bool IsRemote
    {
        get
        {
            var code = git_reference_is_remote(Handle);

            return Git2.ErrorOrBoolean(code);
        }
    }

    public bool IsTag
    {
        get
        {
            var code = git_reference_is_tag(Handle);

            return Git2.ErrorOrBoolean(code);
        }
    }

    public void Dispose()
    {
        git_reference_free(Handle);
    }

    public void Delete()
    {
        Git2.ThrowIfError(git_reference_delete(Handle));
    }

    public string GetName()
    {
        return Utf8StringMarshaller.ConvertToManaged(git_reference_name(Handle));
    }

    public ReferenceHandle Duplicate()
    {
        ReferenceHandle reference;
        Git2.ThrowIfError(git_reference_dup(&reference, Handle));

        return reference;
    }
}
