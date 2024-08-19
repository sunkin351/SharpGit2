namespace SharpGit2.Native;

/// <summary>
/// The contents of one of the files in a binary diff
/// </summary>
public unsafe struct GitDiffBinaryFile
{
    /// <summary>
    /// The type of binary data for this file
    /// </summary>
    public GitDiffBinaryType Type;
    /// <summary>
    /// The binary data, deflated
    /// </summary>
    public byte* Data;
    /// <summary>
    /// The length of the binary data
    /// </summary>
    public nuint DataLength;
    /// <summary>
    /// The length of the binary data after inflation
    /// </summary>
    public nuint InflatedLength;

    public bool TryGetSpan(out ReadOnlySpan<byte> data)
    {
        if (DataLength > int.MaxValue)
        {
            data = default;
            return false;
        }

        data = new(Data, (int)DataLength);
        return true;
    }
}
