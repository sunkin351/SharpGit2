using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpGit2;

public readonly struct RefDBHandle(nint handle) : IDisposable
{
    internal readonly nint NativeHandle = handle;

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}
