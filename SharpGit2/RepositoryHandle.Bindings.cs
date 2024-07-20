using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharpGit2;

unsafe readonly partial struct RepositoryHandle
{
    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    private static partial GitError git_repository_init(RepositoryHandle* repository, string path, uint isBare);

    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial GitError git_repository_open(RepositoryHandle* repository, string path);

    [LibraryImport(Git2.LibraryName)]
    internal static partial void git_repository_free(nint repository);

    [LibraryImport(Git2.LibraryName)]
    internal static partial byte* git_repository_commondir(nint repository);

    [LibraryImport(Git2.LibraryName)]
    internal static partial byte* git_repository_path(nint repository);

    [LibraryImport(Git2.LibraryName)]
    internal static partial byte* git_repository_get_namespace(nint repository);

    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial GitError git_repository_set_namespace(nint repository, string @namespace);

    [LibraryImport(Git2.LibraryName)]
    internal static partial byte* git_repository_workdir(nint repository);

    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial GitError git_repository_set_workdir(nint repository, string workdir);

    [LibraryImport(Git2.LibraryName)]
    internal static partial GitError git_repository_index(IndexHandle* index, nint repository);

    [LibraryImport(Git2.LibraryName)]
    internal static partial GitError git_repository_config(ConfigHandle* config, nint repository);

    [LibraryImport(Git2.LibraryName)]
    internal static partial GitError git_repository_head(ReferenceHandle* head, nint repository);

    [LibraryImport(Git2.LibraryName)]
    internal static partial int git_repository_head_detached(nint repository);

    [LibraryImport(Git2.LibraryName)]
    internal static partial int git_repository_is_bare(nint repository);

    [LibraryImport(Git2.LibraryName)]
    internal static partial GitError git_repository_set_head(nint repository, byte* refname);

    internal static GitError git_repository_set_head(nint repository, string refname)
    {
        scoped Utf8StringMarshaller.ManagedToUnmanagedIn refNameIn = new();
        try
        {
            refNameIn.FromManaged(refname, stackalloc byte[Utf8StringMarshaller.ManagedToUnmanagedIn.BufferSize]);

            return git_repository_set_head(repository, refNameIn.ToUnmanaged());
        }
        finally
        {
            refNameIn.Free();
        }
    }

    [LibraryImport(Git2.LibraryName)]
    internal static partial GitError git_repository_refdb(RefDBHandle* refDB, nint repository);

    [LibraryImport(Git2.LibraryName)]
    internal static partial GitError git_repository_ident(byte** name, byte** email, nint repository);

    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial GitError git_repository_set_ident(nint repository, string name, string email);

    [LibraryImport(Git2.LibraryName)]
    internal static partial int git_repository_state(nint repository);

    [LibraryImport(Git2.LibraryName)]
    internal static partial GitError git_repository_state_cleanup(nint repository);

    [LibraryImport(Git2.LibraryName)]
    internal static partial GitError git_reference_remove(nint repository, byte* name);

    internal static GitError git_reference_remove(nint repository, string name)
    {
        scoped Utf8StringMarshaller.ManagedToUnmanagedIn nameIn = new();
        try
        {
            nameIn.FromManaged(name, stackalloc byte[Utf8StringMarshaller.ManagedToUnmanagedIn.BufferSize]);

            return git_reference_remove(repository, nameIn.ToUnmanaged());
        }
        finally
        {
            nameIn.Free();
        }
    }

    [LibraryImport(Git2.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial GitError git_reference_lookup(ReferenceHandle* reference, nint repository, string name);
}
