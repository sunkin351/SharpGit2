using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpGit2.Tests;

public unsafe sealed class SignatureTests
{
    [Fact]
    public void SignatureTime()
    {
        DateTimeOffset now = DateTimeOffset.Now;

        Native.GitSignature* sig = null;
        Git2.ThrowIfError(GitNativeApi.git_signature_now(&sig, "Your Name", "noreply@gmail.com"));
        try
        {
            var gitNow = (GitTime)now;

            Assert.True(long.Abs((long)sig->When.Time - (long)gitNow.Time) <= 1); // The two time measurements could be in 2 separate seconds
            Assert.Equal(sig->When.Offset, gitNow.Offset);
            Assert.Equal(sig->When.Sign, gitNow.Sign);
        }
        finally
        {
            GitNativeApi.git_signature_free(sig);
        }

        var now2 = (DateTimeOffset)(GitTime)now;

        Assert.Equal(now.ToUnixTimeSeconds(), now2.ToUnixTimeSeconds());
        Assert.Equal(now.TotalOffsetMinutes, now2.TotalOffsetMinutes);
    }

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

        string sigString = sig1.ToString();
        Assert.Matches("[^<>]+<[^<>]+>(\\s+\\d+(\\s+[+-]\\d{4})?)?", sigString);

        Assert.True(GitSignature.TryParse(sigString, out GitSignature sig2));
        Assert.Equal(sig1.Name, sig2.Name);
        Assert.Equal(sig1.Email, sig2.Email);
        Assert.Equal(sig1.When.ToUnixTimeSeconds(), sig2.When.ToUnixTimeSeconds());
        Assert.Equal(sig1.When.TotalOffsetMinutes, sig2.When.TotalOffsetMinutes);

        Native.GitSignature* sig3 = null;
        Git2.ThrowIfError(GitNativeApi.git_signature_from_buffer(&sig3, sigString));

        try
        {
            sig2 = new GitSignature(in *sig3);
        }
        finally
        {
            GitNativeApi.git_signature_free(sig3);
        }

        Assert.Equal(sig1.Name, sig2.Name);
        Assert.Equal(sig1.Email, sig2.Email);
        Assert.Equal(sig1.When.ToUnixTimeSeconds(), sig2.When.ToUnixTimeSeconds());
        Assert.Equal(sig1.When.TotalOffsetMinutes, sig2.When.TotalOffsetMinutes);

        Assert.True(GitSignature.TryParse("X <xyz@gmail.com> 5000000", out sig1));
        Assert.Equal(5000000, sig1.When.ToUnixTimeSeconds());
        Assert.Equal(0, sig1.When.TotalOffsetMinutes);
    }
}
