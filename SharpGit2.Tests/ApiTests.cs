using System.Text;

namespace SharpGit2.Tests;

public unsafe sealed class ApiTests : IDisposable
{
    private readonly DirectoryInfo _directory;
    private GitRepository _repository;

    public ApiTests()
    {
        _directory = Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, "sharpgit2_testrepo"));

        _repository = GitRepository.Init(_directory.FullName);
    }

    public void Dispose()
    {
        _repository.Dispose();
        _repository = default;

        if (OperatingSystem.IsWindows())
        {
            // Readonly files cannot be deleted on windows.
            foreach (var path in Directory.EnumerateFiles(_directory.FullName, "*", SearchOption.AllDirectories))
            {
                var attributes = File.GetAttributes(path);

                if ((attributes & FileAttributes.ReadOnly) != 0)
                    File.SetAttributes(path, attributes & ~FileAttributes.ReadOnly);
            }
        }

        _directory.Delete(true);
    }

    [Fact]
    public void BlobStreamsTest()
    {
        GitObjectID blobId;

        const string content = "This is a test value!";

        using (var stream = _repository.CreateBlobFromStream(null))
        {
            using (var writer = new StreamWriter(stream, encoding: Encoding.UTF8, leaveOpen: true))
            {
                writer.Write(content);
            }

            blobId = stream.Commit();
        }

        using (var blob = _repository.GetBlob(in blobId))
        using (var stream = new StreamReader(blob.GetStream()))
        {
            Assert.Equal(content, stream.ReadToEnd());
        }
    }

    [Fact]
    public void BlobCreateFromBufferTest()
    {
        ReadOnlySpan<byte> content = """
            My Utf8 File Blob!

            This is a multi-line blob!
            """u8;

        GitObjectID id = _repository.CreateBlobFromBuffer(content);

        using (var blob = _repository.GetBlob(in id))
        {
            Assert.True(blob.TryGetContentSpan(out var blobSpan));
            Assert.True(blobSpan.SequenceEqual(content));
        }

        GitObjectID id2 = _repository.CreateBlobFromBuffer(content);

        Assert.Equal(id, id2);
    }

    [Fact]
    public void CommitMessageEncodingTest()
    {
        var signature = new GitSignature("My Name", "no-reply@gmail.com", DateTimeOffset.Now);

        GitObjectID blobId, treeId, commitId;
        
        blobId = _repository.CreateBlobFromBuffer("Hello World!\nHello Life!\n"u8);

        using (var builder = new GitTreeBuilder(_repository))
        {
            builder.Insert("HelloWorld.txt", in blobId, GitFileMode.Blob);

            treeId = builder.Write();
        }

        const string message = "Hello World\n";

        using (var tree = _repository.GetTree(in treeId))
        {
            commitId = _repository.CreateCommit("HEAD", signature, signature, message, tree, default);
        }

        using var commit = _repository.GetCommit(in commitId);

        Assert.Equal(message, commit.GetMessage());
        Assert.Matches("[0-9a-fA-F]+", ((GitObject)commit).GetShortID());
    }
}