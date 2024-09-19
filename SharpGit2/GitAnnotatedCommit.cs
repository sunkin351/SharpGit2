using System.Runtime.InteropServices.Marshalling;

using static SharpGit2.NativeApi;

namespace SharpGit2;

public unsafe readonly struct GitAnnotatedCommit(Git2.AnnotatedCommit* nativeHandle) : IDisposable
{
    public readonly Git2.AnnotatedCommit* NativeHandle = nativeHandle;

    public void Dispose()
    {
        git_annotated_commit_free(NativeHandle);
    }

    public ref readonly GitObjectID Id => ref *git_annotated_commit_id(this.NativeHandle);

    public string? GetRefName()
    {
        return Utf8StringMarshaller.ConvertToManaged(git_annotated_commit_ref(this.NativeHandle));
    }
}
