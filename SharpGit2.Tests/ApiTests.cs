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
}