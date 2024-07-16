using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Scripting.Git;
internal readonly struct ConfigHandle(nint handle) : IDisposable
{
    internal readonly nint Handle = handle;

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}