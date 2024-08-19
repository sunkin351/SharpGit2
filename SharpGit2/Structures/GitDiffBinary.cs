namespace SharpGit2.Native;

public unsafe struct GitDiffBinary
{
    public uint _containsData;
    public GitDiffBinaryFile OldFile;
    public GitDiffBinaryFile NewFile;

    public bool ContainsData
    {
        readonly get => _containsData != 0;
        set => _containsData = value ? 1u : 0;
    }
}
