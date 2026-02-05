using System.Security.Cryptography;

namespace SharpGit2.Managed.Tests;

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
}