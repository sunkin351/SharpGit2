using System.Runtime.InteropServices;

namespace SharpGit2;

public unsafe sealed class RepositoryInitOptions
{
    internal Unmanaged _structure;
    private readonly bool _reusable;

    public RepositoryInitOptions()
    {
        _structure.Version = 1;
    }

    public RepositoryInitOptions(bool reusable) : this()
    {
        _reusable = reusable;
    }

    public RepositoryInitFlags Flags
    {
        get => _structure.Flags;
        set => _structure.Flags = value;
    }

    public RepositoryInitMode Mode
    {
        get => _structure.Mode;
        set => _structure.Mode = value;
    }

    private Git2.UnmanagedString _workingDirectory;

    public string? WorkingDirectory
    {
        get => _workingDirectory.Value;
        set
        {
            if (_workingDirectory.SetValue(value, _reusable))
            {
                _structure.WorkingDirectory = _workingDirectory.NativePointer;
            }
        }
    }

    private Git2.UnmanagedString _description;

    public string? Description
    {
        get => _description.Value;
        set
        {
            if (_description.SetValue(value, _reusable))
            {
                _structure.Description = _description.NativePointer;
            }
        }
    }

    private Git2.UnmanagedString _templatePath;

    public string? TemplatePath
    {
        get => _templatePath.Value;
        set
        {
            if (_templatePath.SetValue(value, _reusable))
            {
                _structure.TemplatePath = _templatePath.NativePointer;
            }
        }
    }

    private Git2.UnmanagedString _initialHead;

    public string? InitialHead
    {
        get => _initialHead.Value;
        set
        {
            if (_initialHead.SetValue(value, _reusable))
            {
                _structure.InitialHead = _initialHead.NativePointer;
            }
        }
    }

    private Git2.UnmanagedString _originUrl;

    public string? OriginUrl
    {
        get => _originUrl.Value;
        set
        {
            if (_originUrl.SetValue(value, _reusable))
            {
                _structure.OriginUrl = _originUrl.NativePointer;
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Unmanaged
    {
        public uint Version;
        public RepositoryInitFlags Flags;
        public RepositoryInitMode Mode;
        public byte* WorkingDirectory;
        public byte* Description;
        public byte* TemplatePath;
        public byte* InitialHead;
        public byte* OriginUrl;

#if SHA256_OBJECT_ID
        public GitObjectIDType ObjectIDType;
#endif
    }
}
