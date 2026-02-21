using SharpGit2.Managed.Internal;

namespace SharpGit2.Managed.Tests;

public class FileSystemAPIAssumptions
{
    [Fact]
    public void GetRelativePathTest()
    {
        string rootPath = OperatingSystem.IsWindows() ? "C:/Users/youruser" : "/home/youruser";
        string absolutePath = OperatingSystem.IsWindows() ? "C:/Users/youruser/some/path" : "/home/youruser/some/path";
        
        Assert.Equal("some/path", Path.GetRelativePath(rootPath, absolutePath));
        Assert.Equal("some/path", Path.GetRelativePath(rootPath + "/", absolutePath));
    }
    
    [Fact]
    public void FileInUseTest()
    {
        const string fileName = "./hello-world.txt";
        using var handle1 = File.OpenHandle(fileName, FileMode.CreateNew, FileAccess.ReadWrite,
            FileShare.None, FileOptions.DeleteOnClose);

        var ex = Assert.ThrowsAny<IOException>(() => new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite).Dispose());
        Assert.True(ex.FileInUse);
    }
    
    [Fact]
    public void DirectoryDeleteBehavior()
    {
        const string dir = "./TestDir/";
        
        Directory.CreateDirectory(dir);
        try
        {
            Directory.CreateDirectory(dir + "test1");
            Directory.CreateDirectory(dir + "test2");
            Directory.CreateDirectory(dir + "test3");

            using (var stream = File.Create(dir + "hello-world.txt"))
            {
                stream.Write("hello world"u8);
            }

            Assert.False(FileSystemHelpers.DeleteDirectoryRecursive(dir));

            Assert.False(Directory.Exists(dir + "test1"));
            Assert.False(Directory.Exists(dir + "test2"));
            Assert.False(Directory.Exists(dir + "test3"));
            
            var ex = Assert.Throws<IOException>(() => new FileStream(dir + "hello-world.txt", FileMode.CreateNew).Dispose());
            Assert.True(ex.FileAlreadyExists);
        }
        finally
        {
            Directory.Delete(dir, true); // standard recursive delete that doesn't preserve normal files.
        }
    }
}