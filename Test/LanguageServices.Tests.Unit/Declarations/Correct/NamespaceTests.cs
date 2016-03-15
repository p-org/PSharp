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

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Microsoft.PSharp.LanguageServices.Parsing;

namespace Microsoft.PSharp.LanguageServices.Tests.Unit
{
    [TestClass]
    public class NamespaceTests
    {
        [TestMethod, Timeout(3000)]
        public void TestNamespaceDeclaration()
        {
            var test = @"
namespace Foo { }";

            var tokens = new PSharpLexer().Tokenize(test);
            var program = new PSharpParser(new PSharpProject(),
                SyntaxFactory.ParseSyntaxTree(test), false).ParseTokens(tokens);
            program.Rewrite();

            var expected = @"
using Microsoft.PSharp;
namespace Foo
{
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty),
                program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(3000)]
        public void TestNamespaceDeclaration2()
        {
            var test = @"
namespace Foo { }
namespace Bar { }";

            var tokens = new PSharpLexer().Tokenize(test);
            var program = new PSharpParser(new PSharpProject(),
                SyntaxFactory.ParseSyntaxTree(test), false).ParseTokens(tokens);
            program.Rewrite();

            var expected = @"
using Microsoft.PSharp;
namespace Foo
{
}
namespace Bar
{
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty),
                program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(3000)]
        public void TestNamespaceDeclarationCompact()
        {
            var test = @"
namespace Foo{}";

            var tokens = new PSharpLexer().Tokenize(test);
            var program = new PSharpParser(new PSharpProject(),
                SyntaxFactory.ParseSyntaxTree(test), false).ParseTokens(tokens);
            program.Rewrite();

            var expected = @"
using Microsoft.PSharp;
namespace Foo
{
}";

            Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty),
                program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }
    }
}
