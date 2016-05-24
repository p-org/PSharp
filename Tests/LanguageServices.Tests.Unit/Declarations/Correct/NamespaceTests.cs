//-----------------------------------------------------------------------
// <copyright file="NamespaceTests.cs">
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
    public class NamespaceTests
    {
        [TestMethod, Timeout(10000)]
        public void TestNamespaceDeclaration()
        {
            var test = @"
namespace Foo { }";

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
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty),
                syntaxTree.ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(10000)]
        public void TestNamespaceDeclaration2()
        {
            var test = @"
namespace Foo { }
namespace Bar { }";

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
}
namespace Bar
{
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty),
                syntaxTree.ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(10000)]
        public void TestNamespaceDeclarationCompact()
        {
            var test = @"
namespace Foo{}";

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
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty),
                syntaxTree.ToString().Replace("\n", string.Empty));
        }
    }
}
