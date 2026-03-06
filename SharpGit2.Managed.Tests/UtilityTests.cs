namespace SharpGit2.Managed.Tests;

public class UtilityTests
{
    [Fact]
    public void CRLFTest()
    {
        Assert.True(Utilities.GatherTextStats("\r\n\0\0\0\0\0\0\0\0\0\0\0\0\0\r\n\r\n\0\0\0\0\0\0\0\0\0\0\0\r\n"u8, false, out var stats));
        Assert.Equal(
            new Utilities.TextStats(Utilities.ByteOrderMark.None, 0, 24, 24, 4, 4, 4),
            stats);
    }

    [Theory]
    [InlineData('\0', false, false, false, true, true)]
    [InlineData('\t', true, false, false, false, false)]
    [InlineData('\f', true, false, false, false, false)]
    [InlineData('\v', true, false, false, false, false)]
    [InlineData('\b', true, false, false, false, false)]
    [InlineData('\e', true, false, false, false, false)]
    [InlineData('\r', false, true, false, false, false)]
    [InlineData('\n', false, false, true, false, false)]
    [InlineData('\x7f', false, false, false, false, true)]
    public void CharacterTest(char character, bool printable, bool cr, bool lf, bool nul, bool nonPrintable)
    {
        const int inputLen0 = 8;
        const int inputLen1 = 20;
        const int inputLen2 = 40;
        const int inputLen3 = 70;
        
        Span<byte> input = stackalloc byte[70];
        input.Fill((byte)character);

        Assert.Equal(nul | nonPrintable | cr, Utilities.GatherTextStats(input.Slice(0, inputLen0), false, out var stats));
        Assert.Equal(printable ? inputLen0 : 0, stats.Printable);
        Assert.Equal(cr ? inputLen0 : 0, stats.CR);
        Assert.Equal(lf ? inputLen0 : 0, stats.LF);
        Assert.Equal(nul ? inputLen0 : 0, stats.Nul);
        Assert.Equal(nonPrintable ? inputLen0 : 0, stats.Nonprintable);
        
        Assert.Equal(nul | nonPrintable | cr, Utilities.GatherTextStats(input.Slice(0, inputLen1), false, out stats));
        Assert.Equal(printable ? inputLen1 : 0, stats.Printable);
        Assert.Equal(cr ? inputLen1 : 0, stats.CR);
        Assert.Equal(lf ? inputLen1 : 0, stats.LF);
        Assert.Equal(nul ? inputLen1 : 0, stats.Nul);
        Assert.Equal(nonPrintable ? inputLen1 : 0, stats.Nonprintable);
        
        Assert.Equal(nul | nonPrintable | cr, Utilities.GatherTextStats(input.Slice(0, inputLen2), false, out stats));
        Assert.Equal(printable ? inputLen2 : 0, stats.Printable);
        Assert.Equal(cr ? inputLen2 : 0, stats.CR);
        Assert.Equal(lf ? inputLen2 : 0, stats.LF);
        Assert.Equal(nul ? inputLen2 : 0, stats.Nul);
        Assert.Equal(nonPrintable ? inputLen2 : 0, stats.Nonprintable);
        
        Assert.Equal(nul | nonPrintable | cr, Utilities.GatherTextStats(input.Slice(0, inputLen3), false, out stats));
        Assert.Equal(printable ? inputLen3 : 0, stats.Printable);
        Assert.Equal(cr ? inputLen3 : 0, stats.CR);
        Assert.Equal(lf ? inputLen3 : 0, stats.LF);
        Assert.Equal(nul ? inputLen3 : 0, stats.Nul);
        Assert.Equal(nonPrintable ? inputLen3 : 0, stats.Nonprintable);
    }
    
    [Fact]
    public void PrintableCharactersTest()
    {
        Assert.False(Utilities.GatherTextStats(TestText1, false, out var stats));
        Assert.Equal(TestText1.Length, stats.Printable + stats.LF + stats.CR);
    }

    private static ReadOnlySpan<byte> TestText1
        => """
           This is a test string.
           
           I wonder which line ending it will use for this.
           """u8;
}