using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpGit2;

public unsafe readonly struct ReferenceDatabaseHandle : IDisposable
{
    internal readonly Git2.ReferenceDatabase* NativeHandle;

    internal ReferenceDatabaseHandle(Git2.ReferenceDatabase* handle)
    {
        NativeHandle = handle;
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}
