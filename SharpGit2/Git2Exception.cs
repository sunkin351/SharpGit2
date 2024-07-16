using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Scripting.Git;

internal class Git2Exception : ApplicationException
{
    public GitError ErrorCode { get; }

    public Git2Exception(GitError code)
    {
        ErrorCode = code;
    }
}
