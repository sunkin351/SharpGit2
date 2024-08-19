namespace SharpGit2.Native
{
    public unsafe struct GitWriteStream
    {
        public delegate* unmanaged[Cdecl]<GitWriteStream*, byte*, nuint, int> Write;
        public delegate* unmanaged[Cdecl]<GitWriteStream*, int> Close;
        public delegate* unmanaged[Cdecl]<GitWriteStream*, void> Free;
    }
}
