using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpGit2;

public unsafe readonly struct ConfigHandle : IDisposable
{
    internal readonly Git2.Config* NativeHandle;

    internal ConfigHandle(Git2.Config* handle)
    {
        NativeHandle = handle;
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}