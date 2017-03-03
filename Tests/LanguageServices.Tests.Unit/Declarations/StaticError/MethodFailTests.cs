//-----------------------------------------------------------------------
// <copyright file="MethodFailTests.cs">
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

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Microsoft.PSharp.LanguageServices.Parsing;

namespace Microsoft.PSharp.LanguageServices.Tests.Unit
{
    [TestClass]
    public class MethodFailTests
    {
        [TestMethod, Timeout(10000)]
        public void TestPublicMethodDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
start state S { }
public void Foo() { }
}
}";

            ParsingOptions options = ParsingOptions.CreateDefault()
                .DisableThrowParsingException();
            var parser = new PSharpParser(new PSharpProject(),
                SyntaxFactory.ParseSyntaxTree(test), options);

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual("A machine method cannot be public.",
                parser.GetParsingErrorLog());
        }

        [TestMethod, Timeout(10000)]
        public void TestInternalMethodDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
start state S { }
internal void Foo() { }
}
}";

            ParsingOptions options = ParsingOptions.CreateDefault()
                .DisableThrowParsingException();
            var parser = new PSharpParser(new PSharpProject(),
                SyntaxFactory.ParseSyntaxTree(test), options);

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual("A machine method cannot be internal.",
                parser.GetParsingErrorLog());
        }

        [TestMethod, Timeout(10000)]
        public void TestMethodDeclarationWithoutBrackets()
        {
            var test = @"
namespace Foo {
machine M {
start state S { }
void Foo()
}
}";

            ParsingOptions options = ParsingOptions.CreateDefault()
                .DisableThrowParsingException();
            var parser = new PSharpParser(new PSharpProject(),
                SyntaxFactory.ParseSyntaxTree(test), options);

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual("Expected \"{\" or \";\".",
                parser.GetParsingErrorLog());
        }
    }
}
