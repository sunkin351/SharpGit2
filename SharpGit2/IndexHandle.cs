﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpGit2;

public unsafe readonly struct IndexHandle : IDisposable
{
    internal readonly Git2.Index* NativeHandle;

    internal IndexHandle(Git2.Index* nativeHandle)
    {
        NativeHandle = nativeHandle;
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}
