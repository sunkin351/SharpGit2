using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpGit2.Tests;

public unsafe class ApiTests : IDisposable
{
    private readonly DirectoryInfo _repoDirectory;
    private GitRepository _repository;

    public ApiTests()
    {
        _repoDirectory = Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, "sharpgit2_testrepo"));

        _repository = GitRepository.Init(_repoDirectory.FullName);
    }

    public void Dispose()
    {
        _repository.Dispose();
        _repository = default;

        try
        {
            _repoDirectory.Delete(true);
        }
        catch (UnauthorizedAccessException)
        {
            if (OperatingSystem.IsWindows())
            {
                // Readonly files cannot be deleted on windows.
                foreach (var path in Directory.EnumerateFiles(_repoDirectory.FullName, "*", SearchOption.AllDirectories))
                {
                    File.SetAttributes(path, File.GetAttributes(path) & ~FileAttributes.ReadOnly);
                }

                _repoDirectory.Delete(true);
            }
            else
            {
                throw;
            }
        }
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

        using (var blob = (GitBlob)_repository.GetObject(in blobId, GitObjectType.Blob))
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

        using (var blob = (GitBlob)_repository.GetObject(in id, GitObjectType.Blob))
        {
            Assert.True(blob.TryGetContentSpan(out var blobSpan));
            Assert.True(blobSpan.SequenceEqual(content));
        }

        GitObjectID id2 = _repository.CreateBlobFromBuffer(content);

        Assert.Equal(id, id2);
    }
}