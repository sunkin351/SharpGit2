using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;

using static SharpGit2.GitNativeApi;

namespace SharpGit2;

public unsafe readonly struct GitTree(Git2.Tree* nativeHandle) : IGitHandle, IGitObject<GitTree>
{
    public Git2.Tree* NativeHandle { get; } = nativeHandle;

    public bool IsNull => this.NativeHandle == null;

    public void Dispose()
    {
        git_tree_free(this.NativeHandle);
    }

    public GitRepository Owner
    {
        get
        {
            var handle = this.ThrowIfNull();

            return new(git_tree_owner(handle.NativeHandle));
        }
    }

    public ref readonly GitObjectID Id
    {
        get
        {
            var handle = this.ThrowIfNull();

            return ref *git_tree_id(handle.NativeHandle);
        }
    }

    public nuint NativeEntryCount
    {
        get
        {
            var handle = this.ThrowIfNull();

            return git_tree_entrycount(handle.NativeHandle);
        }
    }

    public int EntryCount
    {
        get
        {
            var handle = this.ThrowIfNull();

            return checked((int)handle.NativeEntryCount);
        }
    }

    public GitTree Duplicate()
    {
        var handle = this.ThrowIfNull();

        Git2.Tree* tree = null;
        Git2.ThrowIfError(git_tree_dup(&tree, handle.NativeHandle));

        return new(tree);
    }

    /// <summary>
    /// Lookup a tree entry by it's SHA value
    /// </summary>
    /// <param name="id">The SHA value</param>
    /// <returns>The Tree Entry, or null if one isn't found</returns>
    /// <remarks>
    /// The returned entry is owned by it's parent tree!
    /// Do not dispose! And do not use after disposing the parent tree!
    /// </remarks>
    public GitTreeEntry? GetEntryById(in GitObjectID id)
    {
        var handle = this.ThrowIfNull();

        Git2.TreeEntry* entry = null;
        fixed (GitObjectID* pId = &id)
        {
            entry = git_tree_entry_byid(handle.NativeHandle, pId);
        }

        return entry is null ? null : new(entry);
    }

    public GitTreeEntry? GetEntryByIndex(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        return this.GetEntryByIndex((nuint)index);
    }

    /// <summary>
    /// Lookup a tree entry by its position in the tree
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public GitTreeEntry? GetEntryByIndex(nuint index)
    {
        var handle = this.ThrowIfNull();

        var entry = git_tree_entry_byindex(handle.NativeHandle, index);

        return entry is null ? null : new(entry);
    }

    public GitTreeEntry? GetEntryByName(string filename)
    {
        var handle = this.ThrowIfNull();

        var entry = git_tree_entry_byname(handle.NativeHandle, filename);

        return entry is null ? null : new(entry);
    }

    public GitTreeEntry GetEntryByPath(string path)
    {
        var handle = this.ThrowIfNull();

        Git2.TreeEntry* tmp = null;
        Git2.ThrowIfError(git_tree_entry_bypath(&tmp, handle.NativeHandle, path));

        return new(tmp);
    }

    public bool TryGetEntryByPath(string path, out GitTreeEntry entry)
    {
        var handle = this.ThrowIfNull();

        Git2.TreeEntry* tmp = null;
        var error = git_tree_entry_bypath(&tmp, handle.NativeHandle, path);

        switch (error)
        {
            case GitError.OK:
                entry = new(tmp);
                return true;
            case GitError.NotFound:
                entry = default;
                return false;
            default:
                throw Git2.ExceptionForError(error);
        }
    }

    public GitObject GetObjectByPath(string path, GitObjectType type)
    {
        var handle = this.ThrowIfNull();

        Git2.Object* result = null;
        Git2.ThrowIfError(git_object_lookup_bypath(&result, (Git2.Object*)handle.NativeHandle, path, type));

        return new(result);
    }

    public TObject GetObjectByPath<TObject>(string path)
        where TObject : struct, IGitHandle, IGitObject<TObject>
    {
        var handle = this.ThrowIfNull();

        Git2.Object* result = null;
        Git2.ThrowIfError(git_object_lookup_bypath(&result, (Git2.Object*)handle.NativeHandle, path, TObject.ObjectType));

        return TObject.FromObjectPointer(result);
    }

    public bool TryGetObjectByPath(string path, GitObjectType type, out GitObject obj)
    {
        var handle = this.ThrowIfNull();

        Git2.Object* result = null;
        var error = git_object_lookup_bypath(&result, (Git2.Object*)handle.NativeHandle, path, type);

        switch (error)
        {
            case GitError.OK:
                obj = new(result);
                return true;
            case GitError.NotFound:
                obj = default;
                return false;
            default:
                throw Git2.ExceptionForError(error);
        }
    }

    public bool TryGetObjectByPath<TObject>(string path, out TObject obj)
        where TObject : struct, IGitHandle, IGitObject<TObject>
    {
        var handle = this.ThrowIfNull();

        Git2.Object* result = null;
        var error = git_object_lookup_bypath(&result, (Git2.Object*)handle.NativeHandle, path, TObject.ObjectType);

        switch (error)
        {
            case GitError.OK:
                obj = TObject.FromObjectPointer(result);
                return true;
            case GitError.NotFound:
                obj = default;
                return false;
            default:
                throw Git2.ExceptionForError(error);
        }
    }

    public void WalkTree(GitTreeWalkMode mode, Func<string, GitTreeEntry, int> callback)
    {
        var handle = this.ThrowIfNull();

        ArgumentNullException.ThrowIfNull(callback);

        var context = new Git2.CallbackContext<Func<string, GitTreeEntry, int>>() { Callback = callback };
        GitError error = git_tree_walk(handle.NativeHandle, mode, &_Callback, (nint)(void*)&context);

        context.ExceptionInfo?.Throw();
        if (error is < 0 and not GitError.User)
        {
            Git2.ThrowError(error);
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int _Callback(byte* pRoot, Git2.TreeEntry* pEntry, nint payload)
        {
            ref var context = ref *(Git2.CallbackContext<Func<string, GitTreeEntry, int>>*)payload;

            try
            {
                string root = Git2.GetPooledString(pRoot);
                GitTreeEntry entry = new(pEntry);

                var result = context.Callback(root, entry);

                return result < 0 ? (int)GitError.User : result;
            }
            catch (Exception e)
            {
                Debug.Assert(context.ExceptionInfo is null);
                context.ExceptionInfo = ExceptionDispatchInfo.Capture(e);
                return -1;
            }
        }
    }

    public static explicit operator GitTree(GitObject obj)
    {
        return obj.IsNull || obj.Type == GitObjectType.Tree
            ? new GitTree((Git2.Tree*)obj.NativeHandle)
            : throw new InvalidCastException("Git Object is not of type Tree!");
    }

    public static implicit operator GitObject(GitTree tree)
    {
        return new((Git2.Object*)tree.NativeHandle);
    }

    Git2.Object* IGitObject<GitTree>.NativeHandle => (Git2.Object*)this.NativeHandle;

    static GitTree IGitObject<GitTree>.FromObjectPointer(Git2.Object* obj)
    {
        return new((Git2.Tree*)obj);
    }

    static GitObjectType IGitObject<GitTree>.ObjectType => GitObjectType.Tree;
}
