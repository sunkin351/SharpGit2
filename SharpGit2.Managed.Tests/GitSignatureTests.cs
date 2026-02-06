namespace SharpGit2.Managed.Tests;

public class GitSignatureTests
{
    [Fact]
    public void SignatureParse()
    {
        Assert.True(GitSignature.TryParse("X <xyz@gmail.com>", out GitSignature sig1));
        Assert.Equal("X", sig1.Name);
        Assert.Equal("xyz@gmail.com", sig1.Email);
        Assert.Equal(default, sig1.When);

        Assert.False(GitSignature.TryParse("X<xyz@gmail.com>", out _));
        Assert.False(GitSignature.TryParse("X <xyz@gmail.com>5000000", out _));
        Assert.False(GitSignature.TryParse("X <xyz@gmail.com>5000000+0700", out _));
        Assert.False(GitSignature.TryParse("X <xyz@gmail.com> 5000000+0700", out _));
        Assert.False(GitSignature.TryParse("X <xyz@gmail.com>5000000 +0700", out _));

        sig1 = GitSignature.Now(sig1.Name, sig1.Email);

        string? sigString = sig1.ToString();
        Assert.Matches(@"[^<>]+<[^<>]+>(\s+\d+(\s+[+-]\d{4})?)?", sigString);

        Assert.True(GitSignature.TryParse(sigString, out GitSignature sig2));
        Assert.Equal(sig1.Name, sig2.Name);
        Assert.Equal(sig1.Email, sig2.Email);
        Assert.Equal(sig1.When.ToUnixTimeSeconds(), sig2.When.ToUnixTimeSeconds());
        Assert.Equal(sig1.When.TotalOffsetMinutes, sig2.When.TotalOffsetMinutes);

        Assert.True(GitSignature.TryParse("X <xyz@gmail.com> 5000000", out sig1));
        Assert.Equal(5000000, sig1.When.ToUnixTimeSeconds());
        Assert.Equal(0, sig1.When.TotalOffsetMinutes);
    }
    
    [Fact]
    public void MaxDateTimeOffsetTest()
    {
        var sig = new GitSignature("John Doe", "john_doe@unknown.net", DateTimeOffset.MaxValue);

        string sigStr = sig.ToString();
        
        Assert.True(GitSignature.TryParse(sigStr, out GitSignature sig2));
        
        Assert.Equal(sig.Name, sig2.Name);
        Assert.Equal(sig.Email, sig2.Email);
        Assert.Equal(sig.When.ToUnixTimeSeconds(), sig2.When.ToUnixTimeSeconds());
    }
}