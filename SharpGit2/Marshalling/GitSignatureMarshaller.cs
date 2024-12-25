using System.Runtime.InteropServices.Marshalling;

using static SharpGit2.GitNativeApi;

namespace SharpGit2.Marshalling;

[CustomMarshaller(typeof(GitSignature), MarshalMode.Default, typeof(GitSignatureMarshaller))]
[CustomMarshaller(typeof(GitSignature?), MarshalMode.Default, typeof(GitSignatureMarshaller))]
public unsafe static class GitSignatureMarshaller
{
    public static Native.GitSignature* ConvertToUnmanaged(GitSignature signature)
    {
        var seconds = signature.When.ToUnixTimeSeconds();
        var offset = signature.When.TotalOffsetMinutes;

        Native.GitSignature* result = null;
        Git2.ThrowIfError(git_signature_new(&result, signature.Name!, signature.Email!, (ulong)seconds, offset));

        return result;
    }

    public static Native.GitSignature* ConvertToUnmanaged(GitSignature? signature)
    {
        return !signature.HasValue ? null : ConvertToUnmanaged(signature.GetValueOrDefault());
    }

    public static void Free(Native.GitSignature* signature)
    {
        git_signature_free(signature);
    }
}
