using System.Runtime.InteropServices;

using SharpGit2.Marshalling;

namespace SharpGit2.Tests;

public unsafe class MarshallingTests
{
    [Fact]
    public void StringArrayMarshalling()
    {
        Native.GitStringArray arr = StringArrayMarshaller.ConvertToUnmanaged(null);

        Assert.True(arr.Strings == null);
        Assert.True(arr.Count == 0);

        arr = StringArrayMarshaller.ConvertToUnmanaged([]); // empty string array

        Assert.True(arr.Strings == null);
        Assert.True(arr.Count == 0);

        string[] data = ["Hello", "World", "!"];
        arr = StringArrayMarshaller.ConvertToUnmanaged(data);
        try
        {
            Assert.Equal(3u, arr.Count);
            Assert.Equal((IEnumerable<string>)data, arr.ToManaged());

            var span = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(arr.Strings[0]);
            Assert.True(span.SequenceEqual("Hello"u8));
            span = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(arr.Strings[1]);
            Assert.True(span.SequenceEqual("World"u8));
            span = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(arr.Strings[2]);
            Assert.True(span.SequenceEqual("!"u8));
        }
        finally
        {
            StringArrayMarshaller.Free(arr);
        }

        StringArrayMarshaller.ManagedToUnmanagedIn _in = default;
        try
        {
            _in.FromManaged(data);

            arr = *_in.ToUnmanaged();

            Assert.Equal(3u, arr.Count);
            Assert.Equal((IEnumerable<string>)data, arr.ToManaged());

            var span = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(arr.Strings[0]);
            Assert.True(span.SequenceEqual("Hello"u8));
            span = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(arr.Strings[1]);
            Assert.True(span.SequenceEqual("World"u8));
            span = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(arr.Strings[2]);
            Assert.True(span.SequenceEqual("!"u8));
        }
        finally
        {
            _in.Free();
        }
    }

    [Fact]
    public void GitTreeUpdateMarshalling()
    {
        GitTreeUpdate update = new()
        {
            Action = GitTreeUpdateType.Remove,
            FileMode = GitFileMode.Blob,
            Id = GitObjectID.Parse("b3fcd33e7e15902706ec6dd7618fa9a5438f7f59"),
            Path = "SharpGit2/Enums.cs"
        }, update2;

        var _update = GitTreeUpdateMarshaller.ConvertToUnmanaged(update);

        try
        {
            Assert.Equal(update.Action, _update.Action);
            Assert.Equal(update.FileMode, _update.FileMode);
            Assert.Equal(update.Id, _update.Id);
            var span = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(_update.Path);
            Assert.True(span.SequenceEqual("SharpGit2/Enums.cs"u8));

            update2 = GitTreeUpdateMarshaller.ConvertToManaged(_update);
        }
        finally
        {
            GitTreeUpdateMarshaller.Free(_update);
        }

        Assert.Equal(update.Action, update2.Action);
        Assert.Equal(update.FileMode, update2.FileMode);
        Assert.Equal(update.Id, update2.Id);
        Assert.Equal(update.Path, update2.Path);
    }
}
