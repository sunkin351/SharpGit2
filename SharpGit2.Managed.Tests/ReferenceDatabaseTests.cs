using System.Diagnostics.CodeAnalysis;

using CommunityToolkit.HighPerformance.Buffers;

using SharpGit2.Managed.ReferenceDB;

namespace SharpGit2.Managed.Tests;

public class ReferenceDatabaseTests
{
    [StringSyntax("regex")]
    private const string _ReferenceLogSerializationTest_Pattern
        = "^[0-9a-fA-F]{40} [0-9a-fA-F]{40} John Doe <johndoe@unknown.com>( \\d+( [+-]\\d{4})?)?\n$";
    
    [Fact]
    public void ReferenceLogSerializationTest()
    {
        GitObjectID oldId = default, newId = default;
        GitSignature signature = new GitSignature("John Doe", "johndoe@unknown.com", DateTimeOffset.Now);
        
        Random.Shared.NextBytes(oldId.Id);
        Random.Shared.NextBytes(newId.Id);

        using (var buffer = new ArrayPoolBufferWriter<char>())
        {
            GitReferenceDatabaseFileSystemBackend.SerializeReflogEntry(buffer, in oldId, in newId, signature, null);
        
            Assert.Matches(_ReferenceLogSerializationTest_Pattern, buffer.WrittenSpan.ToString());
        }

        var writer = new StringWriter();
        
        GitReferenceDatabaseFileSystemBackend.SerializeReflogEntry(writer, in oldId, in newId, signature, null);
        
        Assert.Matches(_ReferenceLogSerializationTest_Pattern, writer.GetStringBuilder().ToString());
    }
}