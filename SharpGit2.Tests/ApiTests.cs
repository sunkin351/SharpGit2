using System.Text;

namespace SharpGit2.Tests;

public unsafe sealed class ApiTests : IDisposable
{
    private readonly DirectoryInfo _directory;
    private GitRepository _repository;
    private GitSignature _signature;

    public ApiTests()
    {
        _directory = new(Path.Combine(Environment.CurrentDirectory, "sharpgit2_testrepo"));

        if (_directory.Exists)
        {
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

        _repository = GitRepository.Init(_directory.FullName, new GitRepositoryInitOptions()
        {
            Flags = GitRepositoryInitFlags.NoReinit | GitRepositoryInitFlags.MakePath,
            InitialHead = "master"
        });
        _signature = new GitSignature("Your Name Here", "no-reply@gmail.com", default);

        // Give an initial commit so the repo isn't left bare for tests.
        using var index = _repository.GetIndex();

        File.WriteAllText(
            Path.Combine(_directory.FullName, "Readme.md"),
            """
            This repository is a repository used for unit testing SharpGit2.
            """,
            Encoding.UTF8);

        index.Add("Readme.md");
        index.Write();

        var treeId = index.WriteTree();
        using (var tree = _repository.GetObject<GitTree>(in treeId))
        {
            _repository.CreateCommit("HEAD", _signature, _signature, "Initial Commit", tree, []);
        }
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
        using (var head = _repository.GetHead(out _))
        using (var baseTree = head.Peel<GitTree>())
        using (var builder = new GitTreeBuilder(_repository, baseTree))
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

    [Fact]
    public void RebaseTest()
    {
        const string fileName = "MyRebaseTest.txt";
        const string upstreamBranchName = "rebase-test-upstream";
        const string localBranchName = "rebase-test-local";

        const string ancestorVersion = """
            if (12 + 3 < 15)
            {
                Console.WriteLine("We have a problem!");
            }
            else
            {
                Console.WriteLine("Everything's good!");
            }
            """;

        const string upstreamVersion = """
            if (12 + 3 < 15)
            {
                Console.WriteLine("A compiler bug has been encountered!");
            }
            else
            {
                Console.WriteLine("Everything's good!");
            }
            """;

        const string localVersion = """
            if (12 + 3 < 15)
            {
                Console.WriteLine("We have a problem!");
            }
            else
            {
                Console.WriteLine("Everything's wonderful!");
            }
            """;

        GitObjectID baseCommitId = default;

        using (var headRef = _repository.GetHead(out bool unborn))
        {
            Assert.False(unborn);
            Assert.True(headRef.TryGetTarget(out baseCommitId));
        }

        var fileLoc = Path.Combine(_directory.FullName, fileName);

        File.WriteAllText(fileLoc, ancestorVersion, Encoding.UTF8);

        using var index = _repository.GetIndex();

        index.Add(fileName);
        index.Write();

        var objectId = index.WriteTree();

        using (var baseCommit = _repository.GetObject<GitCommit>(in baseCommitId))
        using (var tree = _repository.GetObject<GitTree>(in objectId))
        {
            var signature = _signature with { When = DateTimeOffset.Now };

            objectId = _repository.CreateCommit(null, signature, signature, "Initial MyRebaseTest.txt version", tree, [baseCommit]);
        }

        using var ancestorCommit = _repository.GetObject<GitCommit>(in objectId);
        
        _repository.CreateBranch(localBranchName, ancestorCommit, true).Dispose();

        using (var upstreamBranch = _repository.CreateBranch(upstreamBranchName, ancestorCommit, true))
        {
            _repository.SetHead(upstreamBranch);
        }

        File.WriteAllText(fileLoc, upstreamVersion, Encoding.UTF8);

        index.Add(fileName);
        index.Write();

        objectId = index.WriteTree();
        using (var tree = _repository.GetObject<GitTree>(in objectId))
        {
            var signature = _signature with { When = DateTimeOffset.Now };

            _repository.CreateCommit($"refs/heads/{upstreamBranchName}", signature, signature, $"Upstream modification of {fileName}", tree, [ancestorCommit]);
        }

        _repository.Checkout(ancestorCommit);
        _repository.SetHead($"refs/heads/{localBranchName}");

        File.WriteAllText(fileLoc, localVersion, Encoding.UTF8);

        index.Add(fileName);
        index.Write();

        objectId = index.WriteTree();
        using (var tree = _repository.GetObject<GitTree>(in objectId))
        {
            var signature = _signature with { When = DateTimeOffset.Now };

            _repository.CreateCommit("HEAD", signature, signature, $"Local modification of {fileName}", tree, [ancestorCommit]);
        }

        // Test Begin
        using var upstreamReference = _repository.GetReference($"refs/heads/{upstreamBranchName}");
        using var upstreamAnnotated = _repository.GetAnnotatedCommitFromReference(upstreamReference);

        var rebaseOptions = new GitRebaseOptions();
        rebaseOptions.CheckoutOptions.TheirLabel = "upstream";
        rebaseOptions.CheckoutOptions.OurLabel = "local";
        rebaseOptions.CheckoutOptions.CheckoutStrategy = GitCheckoutStrategyFlags.Safe | GitCheckoutStrategyFlags.UseOurs;
        rebaseOptions.MergeOptions.FileFlags = GitMergeFileFlags.DiffPatience;

        using (var rebase = _repository.StartRebase(default, upstreamAnnotated, default, in rebaseOptions))
        {
            try
            {
                Assert.Equal(1u, rebase.OperationCount);

                while (rebase.Next(out _))
                {
                    rebase.Commit(_signature with { When = DateTimeOffset.Now });
                }

                rebase.Finish();
            }
            catch
            {
                rebase.Abort();
                throw;
            }
        }

        GitMergeFileInput ancestor = new() { FileContent = Encoding.UTF8.GetBytes(ancestorVersion) },
            our = new() { FileContent = Encoding.UTF8.GetBytes(upstreamVersion) },
            their = new() { FileContent = Encoding.UTF8.GetBytes(localVersion) };

        GitMergeFileResult result = default;

        Git2.MergeFileContent(ref result, in ancestor, in our, in their, new GitMergeFileOptions() { Flags = GitMergeFileFlags.DiffPatience });

        var resultVersion = Encoding.UTF8.GetString(result.FileContent);

        var readVersion = File.ReadAllText(fileLoc, Encoding.UTF8);

        Assert.Equal(resultVersion, readVersion);
    }
}