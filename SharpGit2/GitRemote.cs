using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

using SharpGit2.Marshalling;

using static SharpGit2.NativeApi;

namespace SharpGit2;

public unsafe readonly struct GitRemote(Git2.Remote* handle) : IDisposable
{
    public readonly Git2.Remote* NativeHandle = handle;

    public void Dispose()
    {
        git_remote_free(this.NativeHandle);
    }

    public GitRepository? Owner
    {
        get
        {
            var ptr = git_remote_owner(this.NativeHandle);

            return ptr is null ? null : new(ptr);
        }
    }

    public bool IsConnected => git_remote_connected(this.NativeHandle);

    public GitRemoteAutoTagOption AutoTagOption => git_remote_autotag(this.NativeHandle);

    public int PruneRefs => git_remote_prune_refs(this.NativeHandle);

    public nuint RefspecCount => git_remote_refspec_count(this.NativeHandle);

    public GitRemote Duplicate()
    {
        Git2.Remote* result = null;

        Git2.ThrowIfError(git_remote_dup(&result, this.NativeHandle));

        return new(result);
    }

    public IDisposable Connect(GitDirection direction)
    {
        Git2.ThrowIfError(git_remote_connect_ext(this.NativeHandle, direction, null));

        return new ConnectionCleanup(this, default);
    }

    public IDisposable Connect(GitDirection direction, in GitRemoteConnectOptions options)
    {
        GCHandle cbhandle = default;

        Native.GitRemoteConnectOptions _options = new();
        List<GCHandle> gchandles = [];
        try
        {
            if (options.Callbacks is { } callbacks)
            {
                cbhandle = GCHandle.Alloc(callbacks);

                _options.Callbacks.Payload = (nint)cbhandle;
                _options.Callbacks.SetDefaultCallbacks(callbacks);
            }

            _options.ProxyOptions.FromManaged(in options.ProxyOptions, gchandles);
            _options.FollowRedirects = options.FollowRedirects;
            _options.CustomHeaders = StringArrayMarshaller.ConvertToUnmanaged(options.CustomHeaders);

            Git2.ThrowIfError(git_remote_connect_ext(this.NativeHandle, direction, &_options));

            return new ConnectionCleanup(this, cbhandle);
        }
        catch
        {
            cbhandle.Free();
            throw;
        }
        finally
        {
            foreach (var handle in gchandles)
            {
                handle.Free();
            }

            _options.Free();
        }
    }

    public string? GetDefaultBranch()
    {
        Native.GitBuffer buffer = default;

        var error = git_remote_default_branch(&buffer, this.NativeHandle);

        switch (error)
        {
            case GitError.OK:
                try
                {
                    return buffer.AsString();
                }
                finally
                {
                    git_buf_dispose(&buffer);
                }
            case GitError.NotFound:
                return null;
            default:
                throw Git2.ExceptionForError(error);
        }
    }

    public void Download(string[] refspecs, in GitFetchOptions options)
    {
        Native.GitFetchOptions _options = default;
        List<GCHandle> gchandles = [];
        GitError error;
        try
        {
            _options.FromManaged(in options, gchandles);

            error = git_remote_download(this.NativeHandle, refspecs, &_options);
        }
        finally
        {
            foreach (var handle in gchandles)
            {
                handle.Free();
            }

            _options.Free();
        }

        Git2.ThrowIfError(error);
    }

    public void Fetch(string[]? refspecs, in GitFetchOptions options, string? reflog_message)
    {
        Native.GitFetchOptions _options = default;
        List<GCHandle> gchandles = [];
        GitError error;
        try
        {
            _options.FromManaged(in options, gchandles);

            error = git_remote_fetch(this.NativeHandle, refspecs, &_options, reflog_message);
        }
        finally
        {
            foreach (var handle in gchandles)
            {
                handle.Free();
            }

            _options.Free();
        }

        Git2.ThrowIfError(error);
    }

    public void Upload(string[] refspecs, in GitPushOptions options)
    {
        Native.GitPushOptions _options = default;
        List<GCHandle> gchandles = [];
        GitError error;
        try
        {
            _options.FromManaged(in options, gchandles);

            error = git_remote_upload(this.NativeHandle, refspecs, &_options);
        }
        finally
        {
            foreach (var handle in gchandles)
            {
                handle.Free();
            }

            _options.Free();
        }

        Git2.ThrowIfError(error);
    }

    private void Disconnect()
    {
        Git2.ThrowIfError(git_remote_disconnect(this.NativeHandle));
    }

    public string[] GetFetchRefspecs()
    {
        Native.GitStringArray array = default;
        Git2.ThrowIfError(git_remote_get_fetch_refspecs(&array, this.NativeHandle));

        try
        {
            return array.ToManaged();
        }
        finally
        {
            git_strarray_dispose(&array);
        }
    }

    public string[] GetPushRefspecs()
    {
        Native.GitStringArray array = default;
        Git2.ThrowIfError(git_remote_get_push_refspecs(&array, this.NativeHandle));

        try
        {
            return array.ToManaged();
        }
        finally
        {
            git_strarray_dispose(&array);
        }
    }

    public GitRefSpec GetRefSpec(nuint idx)
    {
        return new(git_remote_get_refspec(this.NativeHandle, idx));
    }

    public GitRemoteHead[] GetAdvertisedReferenceList()
    {
        Native.GitRemoteHead** list = null;
        nuint count = 0;

        Git2.ThrowIfError(git_remote_ls(&list, &count, this.NativeHandle));

        // underlying memory belongs to the remote object, do not dispose.

        var array = new GitRemoteHead[checked((int)count)];

        for (int i = 0; i < array.Length; ++i)
        {
            array[i] = new(in *list[i]);
        }

        return array;
    }

    public string? GetName()
    {
        return Utf8StringMarshaller.ConvertToManaged(git_remote_name(this.NativeHandle));
    }

    public void Prune()
    {
        Git2.ThrowIfError(git_remote_prune(this.NativeHandle, null));
    }
    
    public void Prune(IGitRemoteCallbacks? callbacks)
    {
        if (callbacks is null)
        {
            Prune();
            return;
        }

        var cbhandle = GCHandle.Alloc(callbacks);

        try
        {
            Native.GitRemoteCallbacks _callbacks = default;

            _callbacks.Payload = (nint)cbhandle;
            _callbacks.SetDefaultCallbacks(callbacks);

            Git2.ThrowIfError(git_remote_prune(this.NativeHandle, &_callbacks));
        }
        finally
        {
            cbhandle.Free();
        }
    }

    public void Push(string[] refspecs, in GitPushOptions options)
    {
        Native.GitPushOptions _options = default;
        List<GCHandle> gchandles = [];
        GitError error;
        try
        {
            _options.FromManaged(in options, gchandles);

            error = git_remote_push(this.NativeHandle, refspecs, &_options);
        }
        finally
        {
            foreach (var handle in gchandles)
            {
                handle.Free();
            }

            _options.Free();
        }

        Git2.ThrowIfError(error);
    }

    public string? GetPushUrl()
    {
        return Utf8StringMarshaller.ConvertToManaged(git_remote_pushurl(this.NativeHandle));
    }

    public void SetPushUrl(string url)
    {
        Git2.ThrowIfError(git_remote_set_instance_pushurl(this.NativeHandle, url));
    }

    public void SetUrl(string url)
    {
        Git2.ThrowIfError(git_remote_set_instance_url(this.NativeHandle, url));
    }

    public ref readonly GitIndexerProgress GetStats()
    {
        if (this.NativeHandle == null)
        {
            throw new InvalidOperationException("Null Remote Handle!");
        }

        return ref *git_remote_stats(this.NativeHandle);
    }

    /// <summary>
    /// Cancels the current operation
    /// </summary>
    public void Stop()
    {
        Git2.ThrowIfError(git_remote_stop(this.NativeHandle));
    }

    public void UpdateTips(bool update_fetchhead, GitRemoteAutoTagOption download_tags, string? reflog_message)
    {
        Git2.ThrowIfError(git_remote_update_tips(this.NativeHandle, null, update_fetchhead ? 1 : 0, download_tags, reflog_message));
    }

    public void UpdateTips(IGitRemoteCallbacks? callbacks, bool update_fetchhead, GitRemoteAutoTagOption download_tags, string? reflog_message)
    {
        if (callbacks is null)
        {
            Git2.ThrowIfError(git_remote_update_tips(this.NativeHandle, null, update_fetchhead ? 1 : 0, download_tags, reflog_message));
            return;
        }

        var cbhandle = GCHandle.Alloc(callbacks);

        try
        {
            Native.GitRemoteCallbacks _callbacks = default;

            _callbacks.Payload = (nint)cbhandle;
            _callbacks.SetDefaultCallbacks(callbacks);

            Git2.ThrowIfError(git_remote_update_tips(this.NativeHandle, &_callbacks, update_fetchhead ? 1 : 0, download_tags, reflog_message));
        }
        finally
        {
            cbhandle.Free();
        }
    }

    public void UpdateTips(Native.GitRemoteCallbacks* callbacks, bool update_fetchhead, GitRemoteAutoTagOption download_tags, string? reflog_message)
    {
        Git2.ThrowIfError(git_remote_update_tips(this.NativeHandle, callbacks, update_fetchhead ? 1 : 0, download_tags, reflog_message));
    }

    public string GetUrl()
    {
        return Utf8StringMarshaller.ConvertToManaged(git_remote_url(this.NativeHandle))!;
    }

    public static GitRemote CreateDetached(string url)
    {
        Git2.Remote* result = null;

        Git2.ThrowIfError(git_remote_create_detached(&result, url));

        return new(result);
    }

    public static GitRemote Create(string url, in GitRemoteCreateOptions options)
    {
        Git2.Remote* result = null;
        GitError error;

        Native.GitRemoteCreateOptions _options = default;
        try
        {
            _options.FromManaged(in options);

            error = git_remote_create_with_opts(&result, url, &_options);
        }
        finally
        {
            _options.Free();
        }

        Git2.ThrowIfError(error);

        return new(result);
    }

    public static bool IsValidName(string remoteName)
    {
        return git_remote_is_valid_name(remoteName);
    }

    private sealed class ConnectionCleanup : IDisposable
    {
        private GitRemote _remote;
        private GCHandle _cbhandle;

        public ConnectionCleanup(GitRemote remote, GCHandle handle)
        {
            _remote = remote;
            _cbhandle = handle;
        }

        public void Dispose()
        {
            _remote.Disconnect();
            _cbhandle.Free();
        }
    }
}
