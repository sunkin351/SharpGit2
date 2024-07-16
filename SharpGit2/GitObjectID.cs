using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpGit2;

public unsafe struct GitObjectID
{
    public fixed byte Id[20];
}
