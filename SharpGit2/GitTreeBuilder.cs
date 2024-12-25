using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;

using static SharpGit2.GitNativeApi;

namespace SharpGit2;

/// <summary>
/// Constructor for in-memory trees.
/// </summary>
/// <param name="nativeHandle">The native object pointer</param>
/// <remarks>
/// The tree builder can be used to create or modify trees in memory and write them as tree objects to the database.
/// </remarks>
public unsafe readonly struct GitTreeBuilder(Git2.TreeBuilder* nativeHandle) : IGitHandle
{
    /// <summary>
    /// The native object pointer
    /// </summary>
    public Git2.TreeBuilder* NativeHandle { get; } = nativeHandle;

    /// <inheritdoc/>
    public bool IsNull => this.NativeHandle == null;

    /// <summary>
    /// Create a new tree builder.
    /// </summary>
    /// <param name="repository">Repository in which to store the object</param>
    /// <param name="source">Source tree to initialize the builder</param>
    public GitTreeBuilder(GitRepository repository, GitTree source = default) : this(CreateTreeBuilder(repository, source))
    {
    }

    private static Git2.TreeBuilder* CreateTreeBuilder(GitRepository repository, GitTree source)
    {
        if (repository.NativeHandle == null)
        {
            throw new ArgumentNullException(nameof(repository));
        }

        Git2.TreeBuilder* result = null;
        Git2.ThrowIfError(git_treebuilder_new(&result, repository.NativeHandle, source.NativeHandle));

        return result;
    }

    /// <summary>
    /// The number of entries in the tree
    /// </summary>
    public nuint EntryCount
    {
        get
        {
            var handle = this.ThrowIfNull();

            return git_treebuilder_entrycount(handle.NativeHandle);
        }
    }

    public void Dispose()
    {
        git_treebuilder_free(this.NativeHandle);
    }

    public void Clear()
    {
        var handle = this.ThrowIfNull();

        Git2.ThrowIfError(git_treebuilder_clear(handle.NativeHandle));
    }

    public delegate bool FilterCallback(GitTreeEntry entry);

    public void Filter(FilterCallback callback)
    {
        var handle = this.ThrowIfNull();

        var context = new FilterContext(callback);

        Git2.ThrowIfError(git_treebuilder_filter(handle.NativeHandle, &_Callback, (nint)(void*)&context));

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
                (context.Exceptions ??= []).Add(e);

                return 0;
            }
        }
    }

    public GitTreeEntry? this[string filename]
    {
        get
        {
            var handle = this.ThrowIfNull();

            var ptr = git_treebuilder_get(handle.NativeHandle, filename);

            return ptr == null ? null : new(ptr);
        }
    }

    public GitTreeEntry Insert(string fileName, in GitObjectID id, GitFileMode mode)
    {
        Git2.TreeEntry* result;
        GitError error;
        var handle = this.ThrowIfNull();

        fixed (GitObjectID* _id = &id)
        {
            error = git_treebuilder_insert(&result, handle.NativeHandle, fileName, _id, mode);
        }

        Git2.ThrowIfError(error);
        return new(result);
    }

    public void Remove(string fileName)
    {
        var handle = this.ThrowIfNull();

        Git2.ThrowIfError(git_treebuilder_remove(handle.NativeHandle, fileName));
    }

    public GitObjectID Write()
    {
        var handle = this.ThrowIfNull();

        GitObjectID result = default;
        Git2.ThrowIfError(git_treebuilder_write(&result, handle.NativeHandle));

        return result;
    }

    private struct FilterContext(FilterCallback callback)
    {
        public readonly FilterCallback Callback = callback;
        public List<Exception>? Exceptions;
    }
}
