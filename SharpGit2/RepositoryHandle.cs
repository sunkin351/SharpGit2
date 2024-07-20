using System;
using System.Runtime.InteropServices.Marshalling;
using System.Xml.Linq;

namespace SharpGit2;

public unsafe readonly partial struct RepositoryHandle(nint handle) : IDisposable
{
    internal readonly nint Handle = handle;

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
            var code = git_repository_head_detached(Handle);

            return Git2.ErrorOrBoolean(code);
        }
    }

    public bool IsBare
    {
        get
        {
            var code = git_repository_is_bare(Handle);

            return Git2.ErrorOrBoolean(code);
        }
    }

    public int State
    {
        get
        {
            return git_repository_state(Handle);
        }
    }

    public void Dispose()
    {
        git_repository_free(Handle);
    }

    public void CleanupState()
    {
        Git2.ThrowIfError(git_repository_state_cleanup(Handle));
    }

    /// <summary>
    /// Get the configuration file for this repository.
    /// Needs disposal when no longer needed.
    /// </summary>
    /// <returns>The git config file object.</returns>
    public ConfigHandle GetConfig()
    {
        ConfigHandle config;
        Git2.ThrowIfError(git_repository_config(&config, Handle));

        return config;
    }

    public IndexHandle GetIndex()
    {
        IndexHandle index;
        Git2.ThrowIfError(git_repository_index(&index, Handle));

        return index;
    }

    public GitError GetHead(out ReferenceHandle head)
    {
        ReferenceHandle head_loc;
        var error = git_repository_head(&head_loc, Handle);

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
        Git2.ThrowIfError(git_repository_set_head(Handle, refName));
    }

    public void SetHead(ReferenceHandle reference)
    {
        if (reference.Handle == 0)
            throw new NullReferenceException();

        Git2.ThrowIfError(git_repository_set_head(Handle, ReferenceHandle.git_reference_name(reference.Handle)));
    }

    public string GetPath()
    {
        // returned pointer does not need to be freed by the user
        return Utf8StringMarshaller.ConvertToManaged(git_repository_path(Handle));
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
        return Utf8StringMarshaller.ConvertToManaged(git_repository_get_namespace(Handle));
    }

    public RefDBHandle GetRefDB()
    {
        RefDBHandle refDB;
        Git2.ThrowIfError(git_repository_refdb(&refDB, Handle));

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

        switch (git_reference_lookup(&refLoc, Handle, name))
        {
            case GitError.OK:
                reference = refLoc;
                return true;
            case GitError.NotFound:
            case GitError.InvalidSpec:
                reference = default;
                return false;
            default:
                Git2.ThrowError(git_reference_lookup(&refLoc, Handle, name));
                goto case GitError.NotFound;
        }
    }

    public void RemoveReference(string name)
    {
        Git2.ThrowIfError(git_reference_remove(Handle, name));
    }

    public void RemoveReference(ReferenceHandle reference)
    {
        Git2.ThrowIfError(git_reference_remove(Handle, ReferenceHandle.git_reference_name(reference.Handle)));
    }
}

