using static SharpGit2.GitNativeApi;

namespace SharpGit2;

public unsafe readonly struct GitTag(Git2.Tag* nativeHandle) : IGitHandle, IGitObject<GitTag>
{
    public Git2.Tag* NativeHandle { get; } = nativeHandle;

    public bool IsNull => this.NativeHandle == null;

    public void Dispose()
    {
        git_tag_free(NativeHandle);
    }

    public static explicit operator GitTag(GitObject obj)
    {
        return obj.IsNull || obj.Type == GitObjectType.Tag
            ? new GitTag((Git2.Tag*)obj.NativeHandle)
            : throw new InvalidCastException("Git Object is not of type Tag!");
    }

    public static implicit operator GitObject(GitTag tag)
    {
        return new GitObject((Git2.Object*)tag.NativeHandle);
    }

    Git2.Object* IGitObject<GitTag>.NativeHandle => (Git2.Object*)this.NativeHandle;

    static GitTag IGitObject<GitTag>.FromObjectPointer(Git2.Object* obj)
    {
        return new((Git2.Tag*)obj);
    }

    static GitObjectType IGitObject<GitTag>.ObjectType => GitObjectType.Tag;
}
