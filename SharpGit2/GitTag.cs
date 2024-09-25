using static SharpGit2.NativeApi;

namespace SharpGit2;

public unsafe readonly struct GitTag(Git2.Tag* nativeHandle) : IDisposable
{
    public readonly Git2.Tag* NativeHandle = nativeHandle;

    public void Dispose()
    {
        git_tag_free(NativeHandle);
    }

    public static explicit operator GitTag(GitObject obj)
    {
        return obj.Type == GitObjectType.Tag
            ? new GitTag((Git2.Tag*)obj.NativeHandle)
            : throw new InvalidCastException("Git Object is not of type Tag!");
    }

    public static implicit operator GitObject(GitTag tag)
    {
        return new GitObject((Git2.Object*)tag.NativeHandle);
    }
}
