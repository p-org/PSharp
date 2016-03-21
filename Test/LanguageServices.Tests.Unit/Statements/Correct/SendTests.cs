//-----------------------------------------------------------------------
// <copyright file="SendTests.cs">
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
    public class SendTests : BasePSharpTest
    {
        [TestMethod, Timeout(3000)]
        public void TestSendStatement()
        {
            var test = @"
namespace Foo {
event e1;

machine M {
machine Target;
start state S
{
entry
{
send(this.Target, e1);
}
}
}
}";

            var configuration = Configuration.Create();
            configuration.Verbose = 2;

            var solution = base.GetSolution(test);
            var context = CompilationContext.Create(configuration).LoadSolution(solution);

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
MachineId Target;
[Microsoft.PSharp.Start]
[OnEntry(nameof(psharp_S_on_entry_action))]
class S : MachineState
{
}
protected void psharp_S_on_entry_action()
{
this.Send(this.Target,new e1());
}
}
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty),
                syntaxTree.ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(3000)]
        public void TestSendStatementWithSinglePayload()
        {
            var test = @"
namespace Foo {
event e1 (k:int);

machine M {
machine Target;
start state S
{
entry
{
send(this.Target, e1, 10);
}
}
}
}";

            var configuration = Configuration.Create();
            configuration.Verbose = 2;

            var solution = base.GetSolution(test);
            var context = CompilationContext.Create(configuration).LoadSolution(solution);

            ParsingEngine.Create(context).Run();
            RewritingEngine.Create(context).Run();

            var syntaxTree = context.GetProjects()[0].PSharpPrograms[0].GetSyntaxTree();

            var expected = @"
using Microsoft.PSharp;
namespace Foo
{
class e1 : Event
{
 public int k;
 public e1(int k)
  : base()
 {
  this.k = k;
 }
}

class M : Machine
{
MachineId Target;
[Microsoft.PSharp.Start]
[OnEntry(nameof(psharp_S_on_entry_action))]
class S : MachineState
{
}
protected void psharp_S_on_entry_action()
{
this.Send(this.Target,new e1(10));
}
}
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty),
                syntaxTree.ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(3000)]
        public void TestSendStatementWithDoublePayload()
        {
            var test = @"
namespace Foo {
event e1 (k:int, s:string);

machine M {
machine Target;
start state S
{
entry
{
string s = ""hello"";
send(this.Target, e1, 10, s);
}
}
}
}";

            var configuration = Configuration.Create();
            configuration.Verbose = 2;

            var solution = base.GetSolution(test);
            var context = CompilationContext.Create(configuration).LoadSolution(solution);

            ParsingEngine.Create(context).Run();
            RewritingEngine.Create(context).Run();

            var syntaxTree = context.GetProjects()[0].PSharpPrograms[0].GetSyntaxTree();

            var expected = @"
using Microsoft.PSharp;
namespace Foo
{
class e1 : Event
{
 public int k;
 public string s;
 public e1(int k, string s)
  : base()
 {
  this.k = k;
  this.s = s;
 }
}

class M : Machine
{
MachineId Target;
[Microsoft.PSharp.Start]
[OnEntry(nameof(psharp_S_on_entry_action))]
class S : MachineState
{
}
protected void psharp_S_on_entry_action()
{
string s = ""hello"";
this.Send(this.Target,new e1(10, s));
}
}
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty),
                syntaxTree.ToString().Replace("\n", string.Empty));
        }
    }
}
