//-----------------------------------------------------------------------
// <copyright file="StateGroupTests.cs">
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
    public class StateGroupTests
    {
        [TestMethod, Timeout(10000)]
        public void TestMachineStateGroupDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
start state S1 { }
group G { state S2 { } }
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
class M : Machine
{
[Microsoft.PSharp.Start]
class S1 : MachineState
{
}
class G : StateGroup
{
public class S2 : MachineState
{
}
}
}
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty),
                syntaxTree.ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(10000)]
        public void TestMachineEntryDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
group G {
start state S
{
entry{}
}
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
class M : Machine
{
class G : StateGroup
{
[Microsoft.PSharp.Start]
[OnEntry(nameof(psharp_G_S_on_entry_action))]
public class S : MachineState
{
}
}
protected void psharp_G_S_on_entry_action(){}
}
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty),
                syntaxTree.ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(10000)]
        public void TestMachineExitDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
group G 
{
start state S
{
exit{}
}
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
class M : Machine
{
class G : StateGroup
{
[Microsoft.PSharp.Start]
[OnExit(nameof(psharp_G_S_on_exit_action))]
public class S : MachineState
{
}
}
protected void psharp_G_S_on_exit_action(){}
}
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty).Replace("\n", string.Empty),
                syntaxTree.ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(10000)]
        public void TestMachineEntryAndExitDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
group G
{
start state S
{
entry {}
exit {}
}
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
class M : Machine
{
class G : StateGroup
{
[Microsoft.PSharp.Start]
[OnEntry(nameof(psharp_G_S_on_entry_action))]
[OnExit(nameof(psharp_G_S_on_exit_action))]
public class S : MachineState
{
}
}
protected void psharp_G_S_on_entry_action(){}
protected void psharp_G_S_on_exit_action(){}
}
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty).Replace("\n", string.Empty),
                syntaxTree.ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(10000)]
        public void TestMachineOnEventGotoStateDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
group G
{
start state S1
{
on e goto S2;
}
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
class M : Machine
{
class S2 : MachineState
{
}
class G : StateGroup
{
[Microsoft.PSharp.Start]
[OnEventGotoState(typeof(e), typeof(S2))]
public class S1 : MachineState
{
}
}
}
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty).Replace("\n", string.Empty),
                syntaxTree.ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(10000)]
        public void TestMachineOnEventGotoStateDeclaration2()
        {
            var test = @"
namespace Foo {
machine M {
group G
{
start state S1
{
on e1 goto S2;
on e2 goto S3;
}
state S2 {}
state S3 {}
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
class M : Machine
{
class G : StateGroup
{
[Microsoft.PSharp.Start]
[OnEventGotoState(typeof(e1), typeof(S2))]
[OnEventGotoState(typeof(e2), typeof(S3))]
public class S1 : MachineState
{
}
public class S2 : MachineState
{
}
public class S3 : MachineState
{
}
}
}
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty).Replace("\n", string.Empty),
                syntaxTree.ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(10000)]
        public void TestMachineOnEventGotoStateDeclarationWithBody()
        {
            var test = @"
namespace Foo {
machine M {
group G
{
start state S1
{
on e goto S2 with {}
}
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
class M : Machine
{
class G : StateGroup
{
[Microsoft.PSharp.Start]
[OnEventGotoState(typeof(e), typeof(S2), nameof(psharp_G_S1_e_action))]
public class S1 : MachineState
{
}
}
protected void psharp_G_S1_e_action(){}
}
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty).Replace("\n", string.Empty),
                syntaxTree.ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(10000)]
        public void TestMachineOnEventDoActionDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
group G
{
start state S1
{
on e do Bar;
}
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
class M : Machine
{
class G : StateGroup
{
[Microsoft.PSharp.Start]
[OnEventDoAction(typeof(e), nameof(Bar))]
public class S1 : MachineState
{
}
}
}
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty).Replace("\n", string.Empty),
                syntaxTree.ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(10000)]
        public void TestMachineOnEventDoActionDeclarationWithBody()
        {
            var test = @"
namespace Foo {
machine M {
group G
{
start state S1
{
on e do {}
}
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
class M : Machine
{
class G : StateGroup
{
[Microsoft.PSharp.Start]
[OnEventDoAction(typeof(e), nameof(psharp_G_S1_e_action))]
public class S1 : MachineState
{
}
}
protected void psharp_G_S1_e_action(){}
}
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty).Replace("\n", string.Empty),
                syntaxTree.ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(10000)]
        public void TestMachineOnEventGotoStateAndDoActionDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
group G
{
start state S1
{
on e1 goto S2;
on e2 do Bar;
}
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
class M : Machine
{
class G : StateGroup
{
[Microsoft.PSharp.Start]
[OnEventGotoState(typeof(e1), typeof(S2))]
[OnEventDoAction(typeof(e2), nameof(Bar))]
public class S1 : MachineState
{
}
}
}
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty).Replace("\n", string.Empty),
                syntaxTree.ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(10000)]
        public void TestMachineNestedGroup()
        {
            var test = @"
namespace Foo {
machine M {
group G1 {
group G2 {
start state S1 { entry { jump(S1); } }
}
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
class M : Machine
{
class G1 : StateGroup
{
public class G2 : StateGroup
{
[Microsoft.PSharp.Start]
[OnEntry(nameof(psharp_G1_G2_S1_on_entry_action))]
public class S1 : MachineState
{
}
}
}
protected void psharp_G1_G2_S1_on_entry_action(){ { this.Goto(typeof(G1.G2.S1));return; } }
}
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty).Replace("\n", string.Empty),
                syntaxTree.ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(10000)]
        public void TestMachineNestedGroup2()
        {
            var test = @"
namespace Foo {
machine M {
group G1 {
group G2 {
group G3 {
start state S1 { entry { jump(S1); } }
}
}
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
class M : Machine
{
class G1 : StateGroup
{
public class G2 : StateGroup
{
public class G3 : StateGroup
{
[Microsoft.PSharp.Start]
[OnEntry(nameof(psharp_G1_G2_G3_S1_on_entry_action))]
public class S1 : MachineState
{
}
}
}
}
protected void psharp_G1_G2_G3_S1_on_entry_action(){ { this.Goto(typeof(G1.G2.G3.S1));return; } }
}
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty).Replace("\n", string.Empty),
                syntaxTree.ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(10000)]
        public void TestMachineMultipleGroups()
        {
            var test = @"
namespace Foo {
machine M {
group G1 {
  start state S1 { entry { jump(S1); } }
}
group G2 {
  state S1 { entry { jump(S1); } }
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
class M : Machine
{
class G1 : StateGroup
{
[Microsoft.PSharp.Start]
[OnEntry(nameof(psharp_G1_S1_on_entry_action))]
public class S1 : MachineState
{
}
}
class G2 : StateGroup
{
[OnEntry(nameof(psharp_G2_S1_on_entry_action))]
public class S1 : MachineState
{
}
}
protected void psharp_G1_S1_on_entry_action(){ { this.Goto(typeof(G1.S1));return; } }
protected void psharp_G2_S1_on_entry_action(){ { this.Goto(typeof(G2.S1));return; } }
}
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty).Replace("\n", string.Empty),
                syntaxTree.ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(10000)]
        public void TestMachineMultipleGroups2()
        {
            var test = @"
namespace Foo {
machine M {
group G1 {
  start state S1 { entry { jump(S2); } }
  state S2 { entry { jump(S1); } }
}
group G2 {
  state S1 { entry { jump(S2); } }
  state S2 { entry { jump(S1); } }
  state S3 { entry { jump(G1.S1); } }
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
class M : Machine
{
class G1 : StateGroup
{
[Microsoft.PSharp.Start]
[OnEntry(nameof(psharp_G1_S1_on_entry_action))]
public class S1 : MachineState
{
}
[OnEntry(nameof(psharp_G1_S2_on_entry_action))]
public class S2 : MachineState
{
}
}
class G2 : StateGroup
{
[OnEntry(nameof(psharp_G2_S1_on_entry_action))]
public class S1 : MachineState
{
}
[OnEntry(nameof(psharp_G2_S2_on_entry_action))]
public class S2 : MachineState
{
}
[OnEntry(nameof(psharp_G2_S3_on_entry_action))]
public class S3 : MachineState
{
}
}
protected void psharp_G1_S1_on_entry_action(){ { this.Goto(typeof(G1.S2));return; } }
protected void psharp_G1_S2_on_entry_action(){ { this.Goto(typeof(G1.S1));return; } }
protected void psharp_G2_S1_on_entry_action(){ { this.Goto(typeof(G2.S2));return; } }
protected void psharp_G2_S2_on_entry_action(){ { this.Goto(typeof(G2.S1));return; } }
protected void psharp_G2_S3_on_entry_action(){ { this.Goto(typeof(G1.S1));return; } }
}
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty).Replace("\n", string.Empty),
                syntaxTree.ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(10000)]
        public void TestMachineNestedGroups()
        {
            var test = @"
namespace Foo {
machine M {
group G1 {
  start state S1 { entry { jump(G3.S2); } }
  group G3 {
    state S2 { entry { jump(S1); } }
    state S3 { entry { jump(S2); } }
  }
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
class M : Machine
{
class G1 : StateGroup
{
public class G3 : StateGroup
{
[OnEntry(nameof(psharp_G1_G3_S2_on_entry_action))]
public class S2 : MachineState
{
}
[OnEntry(nameof(psharp_G1_G3_S3_on_entry_action))]
public class S3 : MachineState
{
}
}
[Microsoft.PSharp.Start]
[OnEntry(nameof(psharp_G1_S1_on_entry_action))]
public class S1 : MachineState
{
}
}
protected void psharp_G1_S1_on_entry_action(){ { this.Goto(typeof(G1.G3.S2));return; } }
protected void psharp_G1_G3_S2_on_entry_action(){ { this.Goto(typeof(G1.S1));return; } }
protected void psharp_G1_G3_S3_on_entry_action(){ { this.Goto(typeof(G1.G3.S2));return; } }
}
}
";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty).Replace("\n", string.Empty),
                syntaxTree.ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(10000)]
        public void TestMonitorStateGroupDeclaration()
        {
            var test = @"
namespace Foo {
monitor M {
start state S1 { }
group G { state S2 { } }
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
class M : Monitor
{
[Microsoft.PSharp.Start]
class S1 : MonitorState
{
}
class G : StateGroup
{
public class S2 : MonitorState
{
}
}
}
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty),
                syntaxTree.ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(10000)]
        public void TestMonitorEntryDeclaration()
        {
            var test = @"
namespace Foo {
monitor M {
group G {
start state S
{
entry{}
}
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
class M : Monitor
{
class G : StateGroup
{
[Microsoft.PSharp.Start]
[OnEntry(nameof(psharp_G_S_on_entry_action))]
public class S : MonitorState
{
}
}
protected void psharp_G_S_on_entry_action(){}
}
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty),
                syntaxTree.ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(10000)]
        public void TestMonitorExitDeclaration()
        {
            var test = @"
namespace Foo {
monitor M {
group G 
{
start state S
{
exit{}
}
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
class M : Monitor
{
class G : StateGroup
{
[Microsoft.PSharp.Start]
[OnExit(nameof(psharp_G_S_on_exit_action))]
public class S : MonitorState
{
}
}
protected void psharp_G_S_on_exit_action(){}
}
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty).Replace("\n", string.Empty),
                syntaxTree.ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(10000)]
        public void TestMonitorEntryAndExitDeclaration()
        {
            var test = @"
namespace Foo {
monitor M {
group G
{
start state S
{
entry {}
exit {}
}
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
class M : Monitor
{
class G : StateGroup
{
[Microsoft.PSharp.Start]
[OnEntry(nameof(psharp_G_S_on_entry_action))]
[OnExit(nameof(psharp_G_S_on_exit_action))]
public class S : MonitorState
{
}
}
protected void psharp_G_S_on_entry_action(){}
protected void psharp_G_S_on_exit_action(){}
}
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty).Replace("\n", string.Empty),
                syntaxTree.ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(10000)]
        public void TestMonitorOnEventGotoStateDeclaration()
        {
            var test = @"
namespace Foo {
monitor M {
group G
{
start state S1
{
on e goto S2;
}
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
class M : Monitor
{
class S2 : MonitorState
{
}
class G : StateGroup
{
[Microsoft.PSharp.Start]
[OnEventGotoState(typeof(e), typeof(S2))]
public class S1 : MonitorState
{
}
}
}
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty).Replace("\n", string.Empty),
                syntaxTree.ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(10000)]
        public void TestMonitorOnEventGotoStateDeclaration2()
        {
            var test = @"
namespace Foo {
monitor M {
group G
{
start state S1
{
on e1 goto S2;
on e2 goto S3;
}
state S2 {}
state S3 {}
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
class M : Monitor
{
class G : StateGroup
{
[Microsoft.PSharp.Start]
[OnEventGotoState(typeof(e1), typeof(S2))]
[OnEventGotoState(typeof(e2), typeof(S3))]
public class S1 : MonitorState
{
}
public class S2 : MonitorState
{
}
public class S3 : MonitorState
{
}
}
}
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty).Replace("\n", string.Empty),
                syntaxTree.ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(10000)]
        public void TestMonitorOnEventGotoStateDeclarationWithBody()
        {
            var test = @"
namespace Foo {
monitor M {
group G
{
start state S1
{
on e goto S2 with {}
}
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
class M : Monitor
{
class G : StateGroup
{
[Microsoft.PSharp.Start]
[OnEventGotoState(typeof(e), typeof(S2), nameof(psharp_G_S1_e_action))]
public class S1 : MonitorState
{
}
}
protected void psharp_G_S1_e_action(){}
}
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty).Replace("\n", string.Empty),
                syntaxTree.ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(10000)]
        public void TestMonitorOnEventDoActionDeclaration()
        {
            var test = @"
namespace Foo {
monitor M {
group G
{
start state S1
{
on e do Bar;
}
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
class M : Monitor
{
class G : StateGroup
{
[Microsoft.PSharp.Start]
[OnEventDoAction(typeof(e), nameof(Bar))]
public class S1 : MonitorState
{
}
}
}
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty).Replace("\n", string.Empty),
                syntaxTree.ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(10000)]
        public void TestMonitorOnEventDoActionDeclarationWithBody()
        {
            var test = @"
namespace Foo {
monitor M {
group G
{
start state S1
{
on e do {}
}
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
class M : Monitor
{
class G : StateGroup
{
[Microsoft.PSharp.Start]
[OnEventDoAction(typeof(e), nameof(psharp_G_S1_e_action))]
public class S1 : MonitorState
{
}
}
protected void psharp_G_S1_e_action(){}
}
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty).Replace("\n", string.Empty),
                syntaxTree.ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(10000)]
        public void TestMonitorOnEventGotoStateAndDoActionDeclaration()
        {
            var test = @"
namespace Foo {
monitor M {
group G
{
start state S1
{
on e1 goto S2;
on e2 do Bar;
}
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
class M : Monitor
{
class G : StateGroup
{
[Microsoft.PSharp.Start]
[OnEventGotoState(typeof(e1), typeof(S2))]
[OnEventDoAction(typeof(e2), nameof(Bar))]
public class S1 : MonitorState
{
}
}
}
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty).Replace("\n", string.Empty),
                syntaxTree.ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(10000)]
        public void TestMonitorNestedGroup()
        {
            var test = @"
namespace Foo {
monitor M {
group G1 {
group G2 {
start state S1 { entry { jump(S1); } }
}
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
class M : Monitor
{
class G1 : StateGroup
{
public class G2 : StateGroup
{
[Microsoft.PSharp.Start]
[OnEntry(nameof(psharp_G1_G2_S1_on_entry_action))]
public class S1 : MonitorState
{
}
}
}
protected void psharp_G1_G2_S1_on_entry_action(){ { this.Goto(typeof(G1.G2.S1));return; } }
}
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty).Replace("\n", string.Empty),
                syntaxTree.ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(10000)]
        public void TestMonitorNestedGroup2()
        {
            var test = @"
namespace Foo {
monitor M {
group G1 {
group G2 {
group G3 {
start state S1 { entry { jump(S1); } }
}
}
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
class M : Monitor
{
class G1 : StateGroup
{
public class G2 : StateGroup
{
public class G3 : StateGroup
{
[Microsoft.PSharp.Start]
[OnEntry(nameof(psharp_G1_G2_G3_S1_on_entry_action))]
public class S1 : MonitorState
{
}
}
}
}
protected void psharp_G1_G2_G3_S1_on_entry_action(){ { this.Goto(typeof(G1.G2.G3.S1));return; } }
}
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty).Replace("\n", string.Empty),
                syntaxTree.ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(10000)]
        public void TestMonitorMultipleGroups()
        {
            var test = @"
namespace Foo {
monitor M {
group G1 {
  start state S1 { entry { jump(S1); } }
}
group G2 {
  state S1 { entry { jump(S1); } }
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
class M : Monitor
{
class G1 : StateGroup
{
[Microsoft.PSharp.Start]
[OnEntry(nameof(psharp_G1_S1_on_entry_action))]
public class S1 : MonitorState
{
}
}
class G2 : StateGroup
{
[OnEntry(nameof(psharp_G2_S1_on_entry_action))]
public class S1 : MonitorState
{
}
}
protected void psharp_G1_S1_on_entry_action(){ { this.Goto(typeof(G1.S1));return; } }
protected void psharp_G2_S1_on_entry_action(){ { this.Goto(typeof(G2.S1));return; } }
}
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty).Replace("\n", string.Empty),
                syntaxTree.ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(10000)]
        public void TestMonitorMultipleGroups2()
        {
            var test = @"
namespace Foo {
monitor M {
group G1 {
  start state S1 { entry { jump(S2); } }
  state S2 { entry { jump(S1); } }
}
group G2 {
  state S1 { entry { jump(S2); } }
  state S2 { entry { jump(S1); } }
  state S3 { entry { jump(G1.S1); } }
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
class M : Monitor
{
class G1 : StateGroup
{
[Microsoft.PSharp.Start]
[OnEntry(nameof(psharp_G1_S1_on_entry_action))]
public class S1 : MonitorState
{
}
[OnEntry(nameof(psharp_G1_S2_on_entry_action))]
public class S2 : MonitorState
{
}
}
class G2 : StateGroup
{
[OnEntry(nameof(psharp_G2_S1_on_entry_action))]
public class S1 : MonitorState
{
}
[OnEntry(nameof(psharp_G2_S2_on_entry_action))]
public class S2 : MonitorState
{
}
[OnEntry(nameof(psharp_G2_S3_on_entry_action))]
public class S3 : MonitorState
{
}
}
protected void psharp_G1_S1_on_entry_action(){ { this.Goto(typeof(G1.S2));return; } }
protected void psharp_G1_S2_on_entry_action(){ { this.Goto(typeof(G1.S1));return; } }
protected void psharp_G2_S1_on_entry_action(){ { this.Goto(typeof(G2.S2));return; } }
protected void psharp_G2_S2_on_entry_action(){ { this.Goto(typeof(G2.S1));return; } }
protected void psharp_G2_S3_on_entry_action(){ { this.Goto(typeof(G1.S1));return; } }
}
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty).Replace("\n", string.Empty),
                syntaxTree.ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(10000)]
        public void TestMonitorNestedGroups()
        {
            var test = @"
namespace Foo {
monitor M {
group G1 {
  start state S1 { entry { jump(G3.S2); } }
  group G3 {
    state S2 { entry { jump(S1); } }
    state S3 { entry { jump(S2); } }
  }
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
class M : Monitor
{
class G1 : StateGroup
{
public class G3 : StateGroup
{
[OnEntry(nameof(psharp_G1_G3_S2_on_entry_action))]
public class S2 : MonitorState
{
}
[OnEntry(nameof(psharp_G1_G3_S3_on_entry_action))]
public class S3 : MonitorState
{
}
}
[Microsoft.PSharp.Start]
[OnEntry(nameof(psharp_G1_S1_on_entry_action))]
public class S1 : MonitorState
{
}
}
protected void psharp_G1_S1_on_entry_action(){ { this.Goto(typeof(G1.G3.S2));return; } }
protected void psharp_G1_G3_S2_on_entry_action(){ { this.Goto(typeof(G1.S1));return; } }
protected void psharp_G1_G3_S3_on_entry_action(){ { this.Goto(typeof(G1.G3.S2));return; } }
}
}
";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty).Replace("\n", string.Empty),
                syntaxTree.ToString().Replace("\n", string.Empty));
        }
    }
}
