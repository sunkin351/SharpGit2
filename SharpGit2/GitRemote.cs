using System.Runtime.InteropServices;

using SharpGit2.Marshalling;

using static SharpGit2.GitNativeApi;

namespace SharpGit2;

/// <summary>
/// 
/// </summary>
/// <param name="handle">The native object pointer</param>
public unsafe readonly struct GitRemote(Git2.Remote* handle) : IGitHandle
{
    /// <summary>
    /// The native object pointer
    /// </summary>
    public Git2.Remote* NativeHandle { get; } = handle;

    public bool IsNull => this.NativeHandle == null;

    public void Dispose()
    {
        git_remote_free(this.NativeHandle);
    }

    public GitRepository? Owner
    {
        get
        {
            var handle = this.ThrowIfNull();

            var ptr = git_remote_owner(handle.NativeHandle);

            return ptr is null ? null : new(ptr);
        }
    }

    public bool IsConnected
    {
        get
        {
            var handle = this.ThrowIfNull();

            return git_remote_connected(handle.NativeHandle);
        }
    }

    public GitRemoteAutoTagOption AutoTagOption
    {
        get
        {
            var handle = this.ThrowIfNull();

            return git_remote_autotag(handle.NativeHandle);
        }
    }

    public int PruneRefs
    {
        get
        {
            var handle = this.ThrowIfNull();

            return git_remote_prune_refs(handle.NativeHandle);
        }
    }

    public nuint RefspecCount
    {
        get
        {
            var handle = this.ThrowIfNull();

            return git_remote_refspec_count(handle.NativeHandle);
        }
    }

    public GitRemote Duplicate()
    {
        var handle = this.ThrowIfNull();

        Git2.Remote* result = null;

        Git2.ThrowIfError(git_remote_dup(&result, handle.NativeHandle));

        return new(result);
    }

    public IDisposable Connect(GitDirection direction)
    {
        var handle = this.ThrowIfNull();

        Git2.ThrowIfError(git_remote_connect_ext(handle.NativeHandle, direction, null));

        return new ConnectionCleanup(handle, default);
    }

    public IDisposable Connect(GitDirection direction, in GitRemoteConnectOptions options)
    {
        var handle = this.ThrowIfNull();

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

            Git2.ThrowIfError(git_remote_connect_ext(handle.NativeHandle, direction, &_options));

            return new ConnectionCleanup(handle, cbhandle);
        }
        catch
        {
            cbhandle.Free();
            throw;
        }
        finally
        {
            foreach (var gchandle in gchandles)
            {
                gchandle.Free();
            }

            _options.Free();
        }
    }

    public string? GetDefaultBranch()
    {
        var handle = this.ThrowIfNull();

        Native.GitBuffer buffer = default;

        var error = git_remote_default_branch(&buffer, handle.NativeHandle);

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
        Download(new ReadOnlySpan<string>(refspecs), in options);
    }

    public void Download(ReadOnlySpan<string> refspecs, in GitFetchOptions options)
    {
        var handle = this.ThrowIfNull();

        Native.GitFetchOptions _options = default;
        List<GCHandle> gchandles = [];
        GitError error;
        try
        {
            _options.FromManaged(in options, gchandles);

            error = git_remote_download(handle.NativeHandle, refspecs, &_options);
        }
        finally
        {
            foreach (var gchandle in gchandles)
            {
                gchandle.Free();
            }

            _options.Free();
        }

        Git2.ThrowIfError(error);
    }

    public void Fetch(string[]? refspecs, in GitFetchOptions options, string? reflog_message)
    {
        Fetch(new ReadOnlySpan<string>(refspecs), in options, reflog_message);
    }

    public void Fetch(ReadOnlySpan<string> refspecs, in GitFetchOptions options, string? reflog_message)
    {
        var handle = this.ThrowIfNull();

        Native.GitFetchOptions _options = default;
        List<GCHandle> gchandles = [];
        GitError error;
        try
        {
            _options.FromManaged(in options, gchandles);

            error = git_remote_fetch(handle.NativeHandle, refspecs, &_options, reflog_message);
        }
        finally
        {
            foreach (var gchandle in gchandles)
            {
                gchandle.Free();
            }

            _options.Free();
        }

        Git2.ThrowIfError(error);
    }

    public void Upload(string[] refspecs, in GitPushOptions options)
    {
        Upload(new ReadOnlySpan<string>(refspecs), in options);
    }

    public void Upload(ReadOnlySpan<string> refspecs, in GitPushOptions options)
    {
        var handle = this.ThrowIfNull();

        Native.GitPushOptions _options = default;
        List<GCHandle> gchandles = [];
        GitError error;
        try
        {
            _options.FromManaged(in options, gchandles);

            error = git_remote_upload(handle.NativeHandle, refspecs, &_options);
        }
        finally
        {
            foreach (var gchandle in gchandles)
            {
                gchandle.Free();
            }

            _options.Free();
        }

        Git2.ThrowIfError(error);
    }

    private void Disconnect()
    {
        var handle = this.ThrowIfNull();

        Git2.ThrowIfError(git_remote_disconnect(handle.NativeHandle));
    }

    public string[] GetFetchRefspecs()
    {
        var handle = this.ThrowIfNull();

        Native.GitStringArray array = default;
        Git2.ThrowIfError(git_remote_get_fetch_refspecs(&array, handle.NativeHandle));

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
        var handle = this.ThrowIfNull();

        Native.GitStringArray array = default;
        Git2.ThrowIfError(git_remote_get_push_refspecs(&array, handle.NativeHandle));

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
        var handle = this.ThrowIfNull();

        return new(git_remote_get_refspec(handle.NativeHandle, idx));
    }

    public GitRemoteHead[] GetAdvertisedReferenceList()
    {
        var handle = this.ThrowIfNull();

        Native.GitRemoteHead** list = null;
        nuint count = 0;

        Git2.ThrowIfError(git_remote_ls(&list, &count, handle.NativeHandle));

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
        var handle = this.ThrowIfNull();

        var nativeName = git_remote_name(handle.NativeHandle);

        return nativeName == null ? null : Git2.GetPooledString(nativeName);
    }

    public void Prune()
    {
        var handle = this.ThrowIfNull();

        Git2.ThrowIfError(git_remote_prune(handle.NativeHandle, null));
    }
    
    public void Prune(IGitRemoteCallbacks? callbacks)
    {
        var handle = this.ThrowIfNull();

        if (callbacks is null)
        {
            Git2.ThrowIfError(git_remote_prune(handle.NativeHandle, null));
            return;
        }

        var cbhandle = GCHandle.Alloc(callbacks);

        try
        {
            Native.GitRemoteCallbacks _callbacks = default;

            _callbacks.Payload = (nint)cbhandle;
            _callbacks.SetDefaultCallbacks(callbacks);

            Git2.ThrowIfError(git_remote_prune(handle.NativeHandle, &_callbacks));
        }
        finally
        {
            cbhandle.Free();
        }
    }

    public void Push(string[] refspecs, in GitPushOptions options)
    {
        Push(new ReadOnlySpan<string>(refspecs), in options);
    }

    public void Push(ReadOnlySpan<string> refspecs, in GitPushOptions options)
    {
        var handle = this.ThrowIfNull();

        Native.GitPushOptions _options = default;
        List<GCHandle> gchandles = [];
        GitError error;
        try
        {
            _options.FromManaged(in options, gchandles);

            error = git_remote_push(handle.NativeHandle, refspecs, &_options);
        }
        finally
        {
            foreach (var gchandle in gchandles)
            {
                gchandle.Free();
            }

            _options.Free();
        }

        Git2.ThrowIfError(error);
    }

    public string? GetPushUrl()
    {
        var handle = this.ThrowIfNull();

        var nativeUrl = git_remote_pushurl(handle.NativeHandle);

        return nativeUrl == null ? null : Git2.GetPooledString(nativeUrl);
    }

    public void SetPushUrl(string url)
    {
        var handle = this.ThrowIfNull();

        Git2.ThrowIfError(git_remote_set_instance_pushurl(handle.NativeHandle, url));
    }

    public void SetUrl(string url)
    {
        var handle = this.ThrowIfNull();

        Git2.ThrowIfError(git_remote_set_instance_url(handle.NativeHandle, url));
    }

    public ref readonly GitIndexerProgress GetStats()
    {
        var handle = this.ThrowIfNull();

        return ref *git_remote_stats(handle.NativeHandle);
    }

    /// <summary>
    /// Cancels the current operation
    /// </summary>
    public void Stop()
    {
        var handle = this.ThrowIfNull();

        Git2.ThrowIfError(git_remote_stop(handle.NativeHandle));
    }

    public void UpdateTips(bool update_fetchhead, GitRemoteAutoTagOption download_tags, string? reflog_message)
    {
        var handle = this.ThrowIfNull();

        Git2.ThrowIfError(git_remote_update_tips(handle.NativeHandle, null, update_fetchhead ? 1 : 0, download_tags, reflog_message));
    }

    public void UpdateTips(IGitRemoteCallbacks? callbacks, bool update_fetchhead, GitRemoteAutoTagOption download_tags, string? reflog_message)
    {
        var handle = this.ThrowIfNull();

        if (callbacks is null)
        {
            Git2.ThrowIfError(git_remote_update_tips(handle.NativeHandle, null, update_fetchhead ? 1 : 0, download_tags, reflog_message));
            return;
        }

        var cbhandle = GCHandle.Alloc(callbacks);

        try
        {
            Native.GitRemoteCallbacks _callbacks = default;

            _callbacks.Payload = (nint)cbhandle;
            _callbacks.SetDefaultCallbacks(callbacks);

            Git2.ThrowIfError(git_remote_update_tips(handle.NativeHandle, &_callbacks, update_fetchhead ? 1 : 0, download_tags, reflog_message));
        }
        finally
        {
            cbhandle.Free();
        }
    }

    public void UpdateTips(Native.GitRemoteCallbacks* callbacks, bool update_fetchhead, GitRemoteAutoTagOption download_tags, string? reflog_message)
    {
        var handle = this.ThrowIfNull();

        Git2.ThrowIfError(git_remote_update_tips(handle.NativeHandle, callbacks, update_fetchhead ? 1 : 0, download_tags, reflog_message));
    }

    public string GetUrl()
    {
        var handle = this.ThrowIfNull();

        var nativeUrl = git_remote_url(handle.NativeHandle);

        return Git2.GetPooledString(nativeUrl);
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

    private sealed class ConnectionCleanup(GitRemote remote, GCHandle handle) : IDisposable
    {
        private readonly GitRemote _remote = remote;
        private readonly GCHandle _cbhandle = handle;

        public void Dispose()
        {
            _remote.Disconnect();
            _cbhandle.Free();
        }
    }
}
