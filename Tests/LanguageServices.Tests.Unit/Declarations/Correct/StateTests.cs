//-----------------------------------------------------------------------
// <copyright file="StateTests.cs">
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
    public class StateTests
    {
        [TestMethod, Timeout(10000)]
        public void TestStateDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
start state S1 { }
state S2 { }
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
class S2 : MachineState
{
}
}
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty),
                syntaxTree.ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(10000)]
        public void TestEntryDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
start state S
{
entry{}
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
[Microsoft.PSharp.Start]
[OnEntry(nameof(psharp_S_on_entry_action))]
class S : MachineState
{
}
protected void psharp_S_on_entry_action()
{
}
}
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty),
                syntaxTree.ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(10000)]
        public void TestExitDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
start state S
{
exit{}
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
[Microsoft.PSharp.Start]
[OnExit(nameof(psharp_S_on_exit_action))]
class S : MachineState
{
}
protected void psharp_S_on_exit_action()
{
}
}
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty),
                syntaxTree.ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(10000)]
        public void TestEntryAndExitDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
start state S
{
entry {}
exit {}
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
[Microsoft.PSharp.Start]
[OnEntry(nameof(psharp_S_on_entry_action))]
[OnExit(nameof(psharp_S_on_exit_action))]
class S : MachineState
{
}
protected void psharp_S_on_entry_action()
{
}
protected void psharp_S_on_exit_action()
{
}
}
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty),
                syntaxTree.ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(10000)]
        public void TestOnEventGotoStateDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on e goto S2;
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
[Microsoft.PSharp.Start]
[OnEventGotoState(typeof(e), typeof(S2))]
class S1 : MachineState
{
}
}
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty),
                syntaxTree.ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(10000)]
        public void TestOnEventGotoStateDeclaration2()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on e1 goto S2;
on e2 goto S3;
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
[Microsoft.PSharp.Start]
[OnEventGotoState(typeof(e1), typeof(S2))]
[OnEventGotoState(typeof(e2), typeof(S3))]
class S1 : MachineState
{
}
}
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty),
                syntaxTree.ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(10000)]
        public void TestOnSimpleGenericEventGotoStateDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on e<int> goto S2;
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
[Microsoft.PSharp.Start]
[OnEventGotoState(typeof(e<int>), typeof(S2))]
class S1 : MachineState
{
}
}
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty),
                syntaxTree.ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(10000)]
        public void TestOnComplexGenericEventGotoStateDeclaration()
        {
            var test = @"
using System.Collection.Generic;
namespace Foo {
machine M {
start state S1
{
on e<List<Tuple<bool, object>>, Dictionary<string, float>> goto S2;
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
using System.Collection.Generic;
namespace Foo
{
class M : Machine
{
[Microsoft.PSharp.Start]
[OnEventGotoState(typeof(e<List<Tuple<bool,object>>,Dictionary<string,float>>), typeof(S2))]
class S1 : MachineState
{
}
}
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty),
                syntaxTree.ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(10000)]
        public void TestOnQualifiedEventGotoStateDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on Foo.e goto S2;
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
[Microsoft.PSharp.Start]
[OnEventGotoState(typeof(Foo.e), typeof(S2))]
class S1 : MachineState
{
}
}
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty),
                syntaxTree.ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(10000)]
        public void TestOnEventGotoStateDeclarationWithBody()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on e goto S2 with {}
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
[Microsoft.PSharp.Start]
[OnEventGotoState(typeof(e), typeof(S2), nameof(psharp_S1_e_action))]
class S1 : MachineState
{
}
protected void psharp_S1_e_action()
{
}
}
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty),
                syntaxTree.ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(10000)]
        public void TestOnEventDoActionDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on e do Bar;
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
[Microsoft.PSharp.Start]
[OnEventDoAction(typeof(e), nameof(Bar))]
class S1 : MachineState
{
}
}
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty),
                syntaxTree.ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(10000)]
        public void TestOnEventDoActionDeclaration2()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on e1 do Bar;
on e2 do Baz;
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
[Microsoft.PSharp.Start]
[OnEventDoAction(typeof(e1), nameof(Bar))]
[OnEventDoAction(typeof(e2), nameof(Baz))]
class S1 : MachineState
{
}
}
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty),
                syntaxTree.ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(10000)]
        public void TestOnSimpleGenericEventDoActionDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on e<int> do Bar;
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
[Microsoft.PSharp.Start]
[OnEventDoAction(typeof(e<int>), nameof(Bar))]
class S1 : MachineState
{
}
}
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty),
                syntaxTree.ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(10000)]
        public void TestOnComplexGenericEventDoActionDeclaration()
        {
            var test = @"
using System.Collection.Generic;
namespace Foo {
machine M {
start state S1
{
on e<List<Tuple<bool, object>>, Dictionary<string, float>> do Bar;
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
using System.Collection.Generic;
namespace Foo
{
class M : Machine
{
[Microsoft.PSharp.Start]
[OnEventDoAction(typeof(e<List<Tuple<bool,object>>,Dictionary<string,float>>), nameof(Bar))]
class S1 : MachineState
{
}
}
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty),
                syntaxTree.ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(10000)]
        public void TestOnQualifiedEventDoActionDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on Foo.e do Bar;
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
[Microsoft.PSharp.Start]
[OnEventDoAction(typeof(Foo.e), nameof(Bar))]
class S1 : MachineState
{
}
}
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty),
                syntaxTree.ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(10000)]
        public void TestOnEventDoActionDeclarationWithBody()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on e do {}
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
[Microsoft.PSharp.Start]
[OnEventDoAction(typeof(e), nameof(psharp_S1_e_action))]
class S1 : MachineState
{
}
protected void psharp_S1_e_action()
{
}
}
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty),
                syntaxTree.ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(10000)]
        public void TestOnEventGotoStateAndDoActionDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on e1 goto S2;
on e2 do Bar;
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
[Microsoft.PSharp.Start]
[OnEventGotoState(typeof(e1), typeof(S2))]
[OnEventDoAction(typeof(e2), nameof(Bar))]
class S1 : MachineState
{
}
}
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty),
                syntaxTree.ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(10000)]
        public void TestIgnoreEventDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
start state S
{
ignore e;
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
[Microsoft.PSharp.Start]
[IgnoreEvents(typeof(e))]
class S : MachineState
{
}
}
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty),
                syntaxTree.ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(10000)]
        public void TestIgnoreEventDeclaration2()
        {
            var test = @"
namespace Foo {
machine M {
start state S
{
ignore e1, e2;
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
[Microsoft.PSharp.Start]
[IgnoreEvents(typeof(e1), typeof(e2))]
class S : MachineState
{
}
}
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty), syntaxTree.ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(10000)]
        public void TestDeferEventDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
start state S
{
defer e;
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
[Microsoft.PSharp.Start]
[DeferEvents(typeof(e))]
class S : MachineState
{
}
}
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty),
                syntaxTree.ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(10000)]
        public void TestDeferEventDeclaration2()
        {
            var test = @"
namespace Foo {
machine M {
start state S
{
defer e1,e2;
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
[Microsoft.PSharp.Start]
[DeferEvents(typeof(e1), typeof(e2))]
class S : MachineState
{
}
}
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty),
                syntaxTree.ToString().Replace("\n", string.Empty));
        }
    }
}
