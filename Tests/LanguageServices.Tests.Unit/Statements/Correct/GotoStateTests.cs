//-----------------------------------------------------------------------
// <copyright file="GotoStateTests.cs">
//      Copyright (c) Microsoft Corporation. All rights reserved.
// 
//      THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//      EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//      MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//      IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//      CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//      TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//      SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
//-----------------------------------------------------------------------

using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Microsoft.PSharp.LanguageServices.Compilation;
using Microsoft.PSharp.LanguageServices.Parsing;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.LanguageServices.Tests.Unit
{
    [TestClass]
    public class GotoStateTests
    {
        [TestMethod, Timeout(10000)]
        public void TestGotoStateStatement()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
entry
{
jump(S2);
}
}
state S2
{
}
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Machine
    {
        [Microsoft.PSharp.Start]
        [OnEntry(nameof(psharp_S1_on_entry_action))]
        class S1 : MachineState
        {
        }

        class S2 : MachineState
        {
        }

        protected void psharp_S1_on_entry_action()
        {
            this.Goto(typeof(S2));
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [TestMethod, Timeout(10000)]
        public void TestGotoStateStatementWithPSharpAPI()
        {
            var test = @"
using Microsoft.PSharp;
namespace Foo
{
class M : Machine
{
[Microsoft.PSharp.Start]
[OnEntry(nameof(psharp_S1_on_entry_action))]
class S1 : MachineState
{
}
class S2 : MachineState
{
}
protected void psharp_S1_on_entry_action()
{
this.Goto(typeof(S2));
}
}
}";
            var expected = @"
using Microsoft.PSharp;
namespace Foo
{
class M : Machine
{
[Microsoft.PSharp.Start]
[OnEntry(nameof(psharp_S1_on_entry_action))]
class S1 : MachineState
{
}
class S2 : MachineState
{
}
protected void psharp_S1_on_entry_action()
{
this.Goto(typeof(S2));
}
}
}";
            // Note: original formatting is preserved for CSharp rewriting.
            LanguageTestUtilities.AssertRewritten(expected, test, LanguageTestUtilities.ProgramExtension.CSharp);
        }

        [TestMethod, Timeout(10000)]
        public void TestGotoStateStatementWithPSharpAPI2()
        {
            var test = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Machine
    {
        [Microsoft.PSharp.Start]
        [OnEntry(nameof(psharp_S1_on_entry_action))]
        class S1 : MachineState
        {
        }

        class S2 : MachineState
        {
        }

        protected void psharp_S1_on_entry_action()
        {
            this.Goto(typeof(S2));
        }
    }
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Machine
    {
        [Microsoft.PSharp.Start]
        [OnEntry(nameof(psharp_S1_on_entry_action))]
        class S1 : MachineState
        {
        }

        class S2 : MachineState
        {
        }

        protected void psharp_S1_on_entry_action()
        {
            this.Goto(typeof(S2));
        }
    }
}";
            // Note: original formatting is preserved for CSharp rewriting.
            LanguageTestUtilities.AssertRewritten(expected, test, LanguageTestUtilities.ProgramExtension.CSharp);
        }
    }
}
