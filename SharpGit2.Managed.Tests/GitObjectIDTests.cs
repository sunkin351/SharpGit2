using System.Security.Cryptography;

namespace SharpGit2.Managed.Tests;

// These tests could be expanded further
public class GitObjectIDTests
{
    [Fact]
    public void IsZeroChecksAllBytes()
    {
        GitObjectID id = default;
        
        Assert.True(id.IsZero);
        
        for (int i = 0; i < SHA1.HashSizeInBytes; ++i)
        {
            id = default;
            id.Id[i] = 1;
            
            Assert.False(id.IsZero);
        }
    }

    [Fact]
    public void EqualityTest()
    {
        byte[] bytes = new byte[SHA1.HashSizeInBytes];
        Random.Shared.NextBytes(bytes);
        
        var id0 = new GitObjectID(GitObjectIDType.SHA1, bytes);
        var id1 = id0;
        
        Assert.True(GitObjectID.Equals(in id0, in id1));
    }
}