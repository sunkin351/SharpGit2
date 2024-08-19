using System.Runtime.InteropServices.Marshalling;

namespace SharpGit2.Marshalling;

[CustomMarshaller(typeof(GitTreeUpdate), MarshalMode.Default, typeof(GitTreeUpdateMarshaller))]
internal unsafe static class GitTreeUpdateMarshaller
{
    public static Native.GitTreeUpdate ConvertToUnmanaged(GitTreeUpdate managed)
    {
        Native.GitTreeUpdate value;
        value.Action = managed.Action;
        value.Id = managed.Id;
        value.FileMode = managed.FileMode;
        value.Path = Utf8StringMarshaller.ConvertToUnmanaged(managed.Path);

        return value;
    }

    public static GitTreeUpdate ConvertToManaged(Native.GitTreeUpdate unmanaged)
    {
        GitTreeUpdate value;
        value.Action = unmanaged.Action;
        value.Id = unmanaged.Id;
        value.FileMode = unmanaged.FileMode;
        value.Path = Utf8StringMarshaller.ConvertToManaged(unmanaged.Path)!;

        return value;
    }

    public static void Free(Native.GitTreeUpdate unmanaged)
    {
        Utf8StringMarshaller.Free(unmanaged.Path);
    }
}
