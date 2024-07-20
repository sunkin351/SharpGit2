using System;
using System.Runtime.InteropServices.Marshalling;
using System.Xml.Linq;

namespace SharpGit2;

public unsafe readonly partial struct RepositoryHandle(nint handle) : IDisposable
{
    internal readonly nint NativeHandle = handle;

    public static RepositoryHandle Open(string path)
    {
        RepositoryHandle repository;
        Git2.ThrowIfError(git_repository_open(&repository, path));

        return repository;
    }

    public static RepositoryHandle Init(string path, bool isBare = false)
    {
        RepositoryHandle handle;
        Git2.ThrowIfError(git_repository_init(&handle, path, isBare ? 1u : 0u));

        return handle;
    }

    public bool IsHeadDetached
    {
        get
        {
            var code = git_repository_head_detached(NativeHandle);

            return Git2.ErrorOrBoolean(code);
        }
    }

    public bool IsBare
    {
        get
        {
            var code = git_repository_is_bare(NativeHandle);

            return Git2.ErrorOrBoolean(code);
        }
    }

    public int State
    {
        get
        {
            return git_repository_state(NativeHandle);
        }
    }

    public void Dispose()
    {
        git_repository_free(NativeHandle);
    }

    public void CleanupState()
    {
        Git2.ThrowIfError(git_repository_state_cleanup(NativeHandle));
    }

    /// <summary>
    /// Get the configuration file for this repository.
    /// Needs disposal when no longer needed.
    /// </summary>
    /// <returns>The git config file object.</returns>
    public ConfigHandle GetConfig()
    {
        ConfigHandle config;
        Git2.ThrowIfError(git_repository_config(&config, NativeHandle));

        return config;
    }

    public IndexHandle GetIndex()
    {
        IndexHandle index;
        Git2.ThrowIfError(git_repository_index(&index, NativeHandle));

        return index;
    }

    public GitError GetHead(out ReferenceHandle head)
    {
        ReferenceHandle head_loc;
        var error = git_repository_head(&head_loc, NativeHandle);

        switch (error)
        {
            case GitError.OK:
            case GitError.UnbornBranch:
                head = head_loc;
                return error;
            case GitError.NotFound:
                head = default;
                return error;
            default:
                Git2.ThrowError(error);
                goto case GitError.NotFound;
        }
    }

    public void SetHead(string refName)
    {
        Git2.ThrowIfError(git_repository_set_head(NativeHandle, refName));
    }

    public void SetHead(ReferenceHandle reference)
    {
        if (reference.NativeHandle == 0)
            throw new NullReferenceException();

        Git2.ThrowIfError(git_repository_set_head(NativeHandle, ReferenceHandle.git_reference_name(reference.NativeHandle)));
    }

    public string GetPath()
    {
        // returned pointer does not need to be freed by the user
        return Utf8StringMarshaller.ConvertToManaged(git_repository_path(NativeHandle))!;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// Always allocates a new string.
    /// </remarks>
    /// <returns></returns>
    public string GetNamespace()
    {
        // returned pointer does not need to be freed by the user
        return Utf8StringMarshaller.ConvertToManaged(git_repository_get_namespace(NativeHandle))!;
    }

    public RefDBHandle GetRefDB()
    {
        RefDBHandle refDB;
        Git2.ThrowIfError(git_repository_refdb(&refDB, NativeHandle));

        return refDB;
    }

    /// <summary>
    /// Looks up a reference from the repository
    /// </summary>
    /// <param name="name">Name of the reference object</param>
    /// <param name="reference">The returned reference handle</param>
    /// <returns>true if successful, false if not or if the reference name is malformed</returns>
    /// <exception cref="Git2Exception"/>
    public bool TryLookUp(string name, out ReferenceHandle reference)
    {
        ReferenceHandle refLoc;
        var result = git_reference_lookup(&refLoc, NativeHandle, name);

        switch (result)
        {
            case GitError.OK:
                reference = refLoc;
                return true;
            case GitError.NotFound:
            case GitError.InvalidSpec:
                reference = default;
                return false;
            default:
                Git2.ThrowError(result);
                goto case GitError.NotFound;
        }
    }

    public void RemoveReference(string name)
    {
        Git2.ThrowIfError(git_reference_remove(NativeHandle, name));
    }

    public void RemoveReference(ReferenceHandle reference)
    {
        Git2.ThrowIfError(git_reference_remove(NativeHandle, ReferenceHandle.git_reference_name(reference.NativeHandle)));
    }
}

