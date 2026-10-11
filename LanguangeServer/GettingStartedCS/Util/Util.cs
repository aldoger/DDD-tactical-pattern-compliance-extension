using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GettingStartedCS.Util
{
    public static class Util
    {
        public static bool ContainsId(string name)
        {
            return name.Contains("Id", StringComparison.OrdinalIgnoreCase);
        }
    }
}
