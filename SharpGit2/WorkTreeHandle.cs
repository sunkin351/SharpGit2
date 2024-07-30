using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpGit2;

public unsafe readonly struct WorkTreeHandle
{
    internal readonly Git2.Worktree* NativeHandle;

    internal WorkTreeHandle(Git2.Worktree* handle)
    {
        NativeHandle = handle;
    }


}
