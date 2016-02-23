using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PSharp.SystematicTesting
{
    public static class InvokeArgs
    {
        public static PSharpRuntime runtime;
        public static Action<PSharpRuntime> TestAction;
        public static MethodInfo TestMethod;
    }
}
