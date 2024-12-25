namespace SharpGit2;

/// <summary>
/// Helper interface for specific types of lookups
/// </summary>
/// <typeparam name="TSelf"></typeparam>
public unsafe interface IGitObject<TSelf> : IDisposable
    where TSelf: IGitObject<TSelf>, IGitHandle
{
    Git2.Object* NativeHandle { get; }

    static abstract TSelf FromObjectPointer(Git2.Object* obj);

    static abstract GitObjectType ObjectType { get; }
}
