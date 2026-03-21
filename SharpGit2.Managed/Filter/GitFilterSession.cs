using CommunityToolkit.HighPerformance.Buffers;
using SharpGit2.Managed.Attributes;

namespace SharpGit2.Managed.Filter;

internal sealed class GitFilterSession
{
    public GitFilterOptions Options;
    public GitAttributeSession AttributeSession;
    public ArrayPoolBufferWriter<byte> TempBuffer;
}