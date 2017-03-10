//-----------------------------------------------------------------------
// <copyright file="FieldTests.cs">
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
    public class FieldTests
    {
        [TestMethod, Timeout(10000)]
        public void TestIntFieldDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
int k;
start state S { }
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
int k;
[Microsoft.PSharp.Start]
class S : MachineState
{
}
}
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty),
                syntaxTree.ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(10000)]
        public void TestListFieldDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
List<int> k;
start state S { }
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
List<int> k;
[Microsoft.PSharp.Start]
class S : MachineState
{
}
}
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty),
                syntaxTree.ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(10000)]
        public void TestMachineFieldDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
machine N;
start state S { }
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
MachineId N;
[Microsoft.PSharp.Start]
class S : MachineState
{
}
}
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty),
                syntaxTree.ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(10000)]
        public void TestMachineArrayFieldDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
machine[] MachineArray;
List<machine> MachineList; 
List<machine[]> MachineArrayList; 
start state S { }
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
MachineId[] MachineArray;
List<MachineId> MachineList;
List<MachineId[]> MachineArrayList;
[Microsoft.PSharp.Start]
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
