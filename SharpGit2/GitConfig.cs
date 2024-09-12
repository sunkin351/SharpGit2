using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;

#pragma warning disable IDE1006

namespace SharpGit2;

public unsafe readonly struct GitConfig : IDisposable
{
    internal readonly Git2.Config* NativeHandle;

    internal GitConfig(Git2.Config* handle)
    {
        NativeHandle = handle;
    }

    public void Dispose()
    {
        NativeApi.git_config_free(NativeHandle);
    }

    public void AddFile(string path, GitConfigLevel level, GitRepository repository, bool force)
    {
        Git2.ThrowIfError(NativeApi.git_config_add_file_ondisk(NativeHandle, path, level, repository.NativeHandle, force ? 1 : 0));
    }

    public void DeleteEntry(string name)
    {
        Git2.ThrowIfError(NativeApi.git_config_delete_entry(NativeHandle, name));
    }

    public void DeleteMultiVariable(string name, [StringSyntax("regex")] string regex)
    {
        Git2.ThrowIfError(NativeApi.git_config_delete_multivar(NativeHandle, name, regex));
    }

    public Enumerable EnumerateEntries([StringSyntax("regex")] string? regex = null)
    {
        return new Enumerable(this, regex);
    }

    public MultiVariableEnumerable EnumerateMultiVariableEntries(string name, [StringSyntax("regex")] string? regex = null)
    {
        return new MultiVariableEnumerable(this, name, regex);
    }

    public delegate bool ForEachCallback(GitConfigEntry entry);

    public void ForEach(ForEachCallback callback)
    {
        var context = new Git2.CallbackContext<ForEachCallback>() { Callback = callback };

        var gcHandle = GCHandle.Alloc(context, GCHandleType.Normal);
        GitError error;

        try
        {
            error = NativeApi.git_config_foreach(NativeHandle, &_ForEachCallback, GCHandle.ToIntPtr(gcHandle));
        }
        finally
        {
            gcHandle.Free();
        }

        if (error == Git2.ForEachException)
        {
            context.ExceptionInfo!.Throw();
        }
        else if (error < 0)
        {
            Git2.ThrowError(error);
        }
    }

    public void ForEach([StringSyntax("regex")] string regex, ForEachCallback callback)
    {
        var context = new Git2.CallbackContext<ForEachCallback>() { Callback = callback };

        var gcHandle = GCHandle.Alloc(context, GCHandleType.Normal);
        GitError error;

        try
        {
            error = NativeApi.git_config_foreach_match(NativeHandle, regex, &_ForEachCallback, GCHandle.ToIntPtr(gcHandle));
        }
        finally
        {
            gcHandle.Free();
        }

        if (error == Git2.ForEachException)
        {
            context.ExceptionInfo!.Throw();
        }
        else if (error < 0)
        {
            Git2.ThrowError(error);
        }
    }

    public void ForEachMultiVariable(string name, [StringSyntax("regex")] string? regex, ForEachCallback callback)
    {
        var context = new Git2.CallbackContext<ForEachCallback>() { Callback = callback };

        var gcHandle = GCHandle.Alloc(context, GCHandleType.Normal);
        GitError error;

        try
        {
            error = NativeApi.git_config_get_multivar_foreach(NativeHandle, name, regex, &_ForEachCallback, GCHandle.ToIntPtr(gcHandle));
        }
        finally
        {
            gcHandle.Free();
        }

        if (error == Git2.ForEachException)
        {
            context.ExceptionInfo!.Throw();
        }
        else if (error < 0)
        {
            Git2.ThrowError(error);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static GitError _ForEachCallback(Native.GitConfigEntry* entry, nint payload)
    {
        var context = (Git2.CallbackContext<ForEachCallback>)((GCHandle)payload).Target!;

        try
        {
            return context.Callback(new(entry)) ? GitError.OK : Git2.ForEachBreak;
        }
        catch (Exception e)
        {
            Debug.Assert(context.ExceptionInfo is null);
            context.ExceptionInfo = ExceptionDispatchInfo.Capture(e);
            return Git2.ForEachException;
        }
    }

    public bool GetBoolean(string name)
    {
        int value;
        Git2.ThrowIfError(NativeApi.git_config_get_bool(&value, NativeHandle, name));

        return value != 0;
    }

    public GitConfigEntry GetEntry(string name)
    {
        Native.GitConfigEntry* entry = null;
        Git2.ThrowIfError(NativeApi.git_config_get_entry(&entry, NativeHandle, name));

        try
        {
            // Convert to managed representation
            // Contains handling for multiple library versions
            return new GitConfigEntry(entry); 
        }
        finally
        {
            NativeApi.git_config_entry_free(entry);
        }
    }

    public int GetInt32(string name)
    {
        int value;
        Git2.ThrowIfError(NativeApi.git_config_get_int32(&value, NativeHandle, name));

        return value;
    }

    public long GetInt64(string name)
    {
        long value;
        Git2.ThrowIfError(NativeApi.git_config_get_int64(&value, NativeHandle, name));

        return value;
    }

    public string GetPath(string name)
    {
        Native.GitBuffer buffer = default;
        Git2.ThrowIfError(NativeApi.git_config_get_path(&buffer, NativeHandle, name));

        try
        {
            return buffer.AsString();
        }
        finally
        {
            NativeApi.git_buf_dispose(&buffer);
        }
    }

    public GitConfig GetSnapshot()
    {
        Git2.Config* config;
        Git2.ThrowIfError(NativeApi.git_config_snapshot(&config, NativeHandle));

        return new(config);
    }

    public string GetString(string name)
    {
        Native.GitBuffer buffer = default;
        Git2.ThrowIfError(NativeApi.git_config_get_string_buf(&buffer, NativeHandle, name));

        try
        {
            return buffer.AsString();
        }
        finally
        {
            NativeApi.git_buf_dispose(&buffer);
        }
    }

    public void Set(string name, bool value)
    {
        Git2.ThrowIfError(NativeApi.git_config_set_bool(NativeHandle, name, value ? 1 : 0));
    }

    public void Set(string name, int value)
    {
        Git2.ThrowIfError(NativeApi.git_config_set_int32(NativeHandle, name, value));
    }

    public void Set(string name, long value)
    {
        Git2.ThrowIfError(NativeApi.git_config_set_int64(NativeHandle, name, value));
    }

    public void Set(string name, [StringSyntax("regex")] string regex, string value)
    {
        Git2.ThrowIfError(NativeApi.git_config_set_multivar(NativeHandle, name, regex, value));
    }

    public void Set(string name, string value)
    {
        Git2.ThrowIfError(NativeApi.git_config_set_string(NativeHandle, name, value));
    }

    public readonly struct Enumerable : IEnumerable<GitConfigEntry>
    {
        private readonly GitConfig _handle;
        private readonly string? _regex;

        internal Enumerable(GitConfig handle, string? regex)
        {
            _handle = handle;
            _regex = regex;
        }

        public Enumerator GetEnumerator()
        {
            Git2.ConfigIterator* iterator;
            if (_regex is null)
                Git2.ThrowIfError(NativeApi.git_config_iterator_new(&iterator, _handle.NativeHandle));
            else
                Git2.ThrowIfError(NativeApi.git_config_iterator_glob_new(&iterator, _handle.NativeHandle, _regex));

            return new Enumerator(iterator);
        }

        IEnumerator<GitConfigEntry> IEnumerable<GitConfigEntry>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public readonly struct MultiVariableEnumerable : IEnumerable<GitConfigEntry>
    {
        private readonly GitConfig _handle;
        private readonly string _name;
        private readonly string? _regex;

        internal MultiVariableEnumerable(GitConfig handle, string name, string? regex)
        {
            _handle = handle;
            _name = name;
            _regex = regex;
        }

        public Enumerator GetEnumerator()
        {
            Git2.ConfigIterator* iterator;
            Git2.ThrowIfError(NativeApi.git_config_multivar_iterator_new(&iterator, _handle.NativeHandle, _name, _regex));

            return new Enumerator(iterator);
        }

        IEnumerator<GitConfigEntry> IEnumerable<GitConfigEntry>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public struct Enumerator : IEnumerator<GitConfigEntry>
    {
        private Git2.ConfigIterator* _nativeHandle;

        internal Enumerator(Git2.ConfigIterator* nativeHandle)
        {
            _nativeHandle = nativeHandle;
        }

        public GitConfigEntry Current { readonly get; private set; }

        public bool MoveNext()
        {
            ObjectDisposedException.ThrowIf(_nativeHandle is null, typeof(Enumerator));

            Native.GitConfigEntry* entry;
            var error = NativeApi.git_config_next(&entry, _nativeHandle);

            switch (error)
            {
                case GitError.OK:
                    Current = new(entry);
                    return true;
                case GitError.IterationOver:
                    Current = default;
                    return false;
                default:
                    throw Git2.ExceptionForError(error);
            }
        }

        public void Dispose()
        {
            if (_nativeHandle is not null)
            {
                NativeApi.git_config_iterator_free(_nativeHandle);
                _nativeHandle = null;
                Current = default;
            }
        }

        void IEnumerator.Reset()
        {
            throw new NotImplementedException();
        }

        readonly object IEnumerator.Current => this.Current;
    }
}