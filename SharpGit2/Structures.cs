﻿using System.Runtime.InteropServices.Marshalling;

namespace SharpGit2;

/// <summary>
/// An entry in a configuration file
/// </summary>
public unsafe struct GitConfigEntry
{
    /// <summary>
    /// Name of the configuration entry (normalized)
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Literal value of the entry
    /// </summary>
    public string Value { get; init; }

    /// <summary>
    /// The type of backend that this entry exists in (eg, "file")
    /// </summary>
    /// <remarks>
    /// Will be <see langword="null"/> on versions less or equal to 1.7.2
    /// </remarks>
    public string? BackendType { get; init; }

    /// <summary>
    /// The path to the origin of this entry. For config files, this is the path to the file.
    /// </summary>
    /// <remarks>
    /// Will be <see langword="null"/> on versions less or equal to 1.7.2
    /// </remarks>
    public string? OriginPath { get; init; }

    /// <summary>
    /// Depth of includes where this variable was found
    /// </summary>
    public uint IncludeDepth { get; init; }

    /// <summary>
    /// Configuration level for the file this was found in
    /// </summary>
    public GitConfigLevel Level { get; init; }

    internal GitConfigEntry(Unmanaged* ptr)
    {
        // Correctly handle libgit2 versions less or equal to 1.7.2
        if (Git2.NativeLibraryVersion <= new Git2.Version(1, 7, 2))
        {
            var ptr2 = (Unmanaged_1_7_2*)ptr; // actual type, given the lib version

            Name = Utf8StringMarshaller.ConvertToManaged(ptr2->Name)!;
            Value = Utf8StringMarshaller.ConvertToManaged(ptr2->Value)!;
            IncludeDepth = ptr2->IncludeDepth;
            Level = ptr2->Level;
            return;
        }

        Name = Utf8StringMarshaller.ConvertToManaged(ptr->Name)!;
        Value = Utf8StringMarshaller.ConvertToManaged(ptr->Value)!;
        BackendType = Utf8StringMarshaller.ConvertToManaged(ptr->BackendType);
        OriginPath = Utf8StringMarshaller.ConvertToManaged(ptr->OriginPath);
        IncludeDepth = ptr->IncludeDepth;
        Level = ptr->Level;
    }

    internal struct Unmanaged
    {
        public byte* Name;
        public byte* Value;
        public byte* BackendType;
        public byte* OriginPath;
        public uint IncludeDepth;
        public GitConfigLevel Level;
    }

    internal struct Unmanaged_1_7_2
    {
        public byte* Name;
        public byte* Value;
        public uint IncludeDepth;
        public GitConfigLevel Level;
    }
}
