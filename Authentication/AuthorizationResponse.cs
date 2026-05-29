using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommissioningChecklistGenerator.Authentication
{
    [Flags]
    internal enum AuthorizationResponse 
    {
        Unknown = 0,
        Success = 1 << 0,
        Failure = 1 << 1,
        Logout = 1 << 2
    }
}
