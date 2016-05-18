//-----------------------------------------------------------------------
// <copyright file="RaiseTests.cs">
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
    public class RaiseTests
    {
        [TestMethod, Timeout(10000)]
        public void TestEventRaiseStatement()
        {
            var test = @"
namespace Foo {
event e1;

machine M {
start state S1
{
entry
{
raise(e1);
}
on e1 goto S2;
}
state S2
{
}
}
}";

            var configuration = Configuration.Create();
            configuration.Verbose = 2;

            var context = CompilationContext.Create(configuration).LoadSolution(test);

            ParsingEngine.Create(context).Run();
            RewritingEngine.Create(context).Run();

            var syntaxTree = context.GetProjects()[0].PSharpPrograms[0].GetSyntaxTree();

            var expected = @"
using Microsoft.PSharp;
namespace Foo
{
class e1 : Event
{
 public e1()
  : base()
 { }
}

class M : Machine
{
[Microsoft.PSharp.Start]
[OnEntry(nameof(psharp_S1_on_entry_action))]
[OnEventGotoState(typeof(e1), typeof(S2))]
class S1 : MachineState
{
}
class S2 : MachineState
{
}
protected void psharp_S1_on_entry_action()
{
{ this.Raise(new e1());return; }
}
}
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty),
                syntaxTree.ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(10000)]
        public void TestEventRaiseStatementWithPSharpAPI()
        {
            var test = @"
using Microsoft.PSharp;
namespace Foo
{
class e1 : Event
{
 public e1()
  : base()
 { }
}

class M : Machine
{
[Microsoft.PSharp.Start]
[OnEntry(nameof(psharp_S1_on_entry_action))]
[OnEventGotoState(typeof(e1), typeof(S2))]
class S1 : MachineState
{
}
class S2 : MachineState
{
}
protected void psharp_S1_on_entry_action()
{
this.Raise(new e1());
}
}
}";

            var configuration = Configuration.Create();
            configuration.Verbose = 2;

            var context = CompilationContext.Create(configuration).LoadSolution(test, "cs");

            ParsingEngine.Create(context).Run();
            RewritingEngine.Create(context).Run();

            var syntaxTree = context.GetProjects()[0].CSharpPrograms[0].GetSyntaxTree();

            var expected = @"
using Microsoft.PSharp;
namespace Foo
{
class e1 : Event
{
 public e1()
  : base()
 { }
}

class M : Machine
{
[Microsoft.PSharp.Start]
[OnEntry(nameof(psharp_S1_on_entry_action))]
[OnEventGotoState(typeof(e1), typeof(S2))]
class S1 : MachineState
{
}
class S2 : MachineState
{
}
protected void psharp_S1_on_entry_action()
{
{ this.Raise(new e1());return; }
}
}
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty),
                syntaxTree.ToString().Replace(Environment.NewLine, string.Empty));
        }
    }
}
