using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SharpGit2;

public unsafe sealed class RepositoryInitOptions
{
    internal Unmanaged _structure;

    public RepositoryInitOptions()
    {
        _structure.Version = 1;
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

    private string? _workingDirectory;
    private byte[]? _workingDirectoryMemory;

    public string? WorkingDirectory
    {
        get => _workingDirectory;
        set
        {
            Git2.MarshalToStructure(ref _structure.WorkingDirectory, ref _workingDirectoryMemory, ref _workingDirectory, value);
        }
    }

    private string? _description;
    private byte[]? _descriptionMemory;

    public string? Description
    {
        get => _description;
        set
        {
            Git2.MarshalToStructure(ref _structure.Description, ref _descriptionMemory, ref _description, value);
        }
    }


    private string? _templatePath;
    private byte[]? _templatePathMemory;

    public string? TemplatePath
    {
        get => _templatePath;
        set
        {
            Git2.MarshalToStructure(ref _structure.TemplatePath, ref _templatePathMemory, ref _templatePath, value);
        }
    }

    private string? _initialHead;
    private byte[]? _initialHeadMemory;

    public string? InitialHead
    {
        get => _initialHead;
        set
        {
            Git2.MarshalToStructure(ref _structure.InitialHead, ref _initialHeadMemory, ref _initialHead, value);
        }
    }

    private string? _originUrl;
    private byte[]? _originUrlMemory;

    public string? OriginUrl
    {
        get => _originUrl;
        set
        {
            Git2.MarshalToStructure(ref _structure.OriginUrl, ref _originUrlMemory, ref _originUrl, value);
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
