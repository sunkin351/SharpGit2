using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static SharpGit2.NativeApi;

namespace SharpGit2;

public unsafe readonly struct GitMailmap(Git2.MailMap* nativeHandle) : IDisposable
{
    public readonly Git2.MailMap* NativeHandle = nativeHandle;

    public void Dispose()
    {
        git_mailmap_free(this.NativeHandle);
    }
}
