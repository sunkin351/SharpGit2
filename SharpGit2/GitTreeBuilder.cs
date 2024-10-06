using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;

using static SharpGit2.NativeApi;

namespace SharpGit2;

public unsafe readonly struct GitTreeBuilder(Git2.TreeBuilder* nativeHandle) : IDisposable
{
    public readonly Git2.TreeBuilder* NativeHandle = nativeHandle;

    public GitTreeBuilder(GitRepository repository) : this(CreateTreeBuilder(repository, default))
    {
    }

    public GitTreeBuilder(GitRepository repository, GitTree source) : this(CreateTreeBuilder(repository, source))
    {
    }

    private static Git2.TreeBuilder* CreateTreeBuilder(GitRepository repository, GitTree source)
    {
        Git2.TreeBuilder* result = null;
        Git2.ThrowIfError(git_treebuilder_new(&result, repository.NativeHandle, source.NativeHandle));

        return result;
    }

    public nuint EntryCount => git_treebuilder_entrycount(this.NativeHandle);

    public void Dispose()
    {
        git_treebuilder_free(this.NativeHandle);
    }

    public void Clear()
    {
        Git2.ThrowIfError(git_treebuilder_clear(this.NativeHandle));
    }

    public delegate bool FilterCallback(GitTreeEntry entry);

    public void Filter(FilterCallback callback)
    {
        var context = new FilterContext(callback);

        Git2.ThrowIfError(git_treebuilder_filter(this.NativeHandle, &_Callback, (nint)(void*)&context));

        switch (context.Exceptions)
        {
            case [Exception e]:
                ExceptionDispatchInfo.Throw(e);
                break;
            case { Count: > 0 }:
                throw new AggregateException(context.Exceptions);
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int _Callback(Git2.TreeEntry* entry, nint payload)
        {
            ref var context = ref *(FilterContext*)payload;

            try
            {
                bool result = context.Callback(new(entry));

                return Unsafe.As<bool, byte>(ref result);
            }
            catch (Exception e)
            {
                if (context.Exceptions is { } list)
                {
                    list.Add(e);
                }
                else
                {
                    context.Exceptions = [e];
                }

                return 0;
            }
        }
    }

    public GitTreeEntry? this[string filename]
    {
        get
        {
            var ptr = git_treebuilder_get(this.NativeHandle, filename);

            return ptr == null ? null : new(ptr);
        }
    }

    public GitTreeEntry Insert(string fileName, in GitObjectID id, GitFileMode mode)
    {
        Git2.TreeEntry* result;
        GitError error;

        fixed (GitObjectID* _id = &id)
        {
            error = git_treebuilder_insert(&result, this.NativeHandle, fileName, _id, mode);
        }

        Git2.ThrowIfError(error);
        return new(result);
    }

    public void Remove(string fileName)
    {
        Git2.ThrowIfError(git_treebuilder_remove(this.NativeHandle, fileName));
    }

    public GitObjectID Write()
    {
        GitObjectID result = default;
        Git2.ThrowIfError(git_treebuilder_write(&result, this.NativeHandle));

        return result;
    }

    private struct FilterContext(FilterCallback callback)
    {
        public readonly FilterCallback Callback = callback;
        public List<Exception>? Exceptions;
    }
}
