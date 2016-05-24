//-----------------------------------------------------------------------
// <copyright file="StateFailTests.cs">
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
    public class StateFailTests
    {
        [TestMethod, Timeout(10000)]
        public void TestStateDeclarationWithMoreThanOneEntry()
        {
            var test = @"
namespace Foo {
machine M {
start state S
{
entry {}
entry{}
}
}
}";

            ParsingOptions options = ParsingOptions.CreateDefault()
                .DisableThrowParsingException();
            var parser = new PSharpParser(new PSharpProject(),
                SyntaxFactory.ParseSyntaxTree(test), options);

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual("Duplicate entry declaration.",
                parser.GetParsingErrorLog());
        }

        [TestMethod, Timeout(10000)]
        public void TestStateDeclarationWithMoreThanOneExit()
        {
            var test = @"
namespace Foo {
machine M {
start state S
{
exit{}
exit {}
}
}
}";

            ParsingOptions options = ParsingOptions.CreateDefault()
                .DisableThrowParsingException();
            var parser = new PSharpParser(new PSharpProject(),
                SyntaxFactory.ParseSyntaxTree(test), options);

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual("Duplicate exit declaration.",
                parser.GetParsingErrorLog());
        }

        [TestMethod, Timeout(10000)]
        public void TestEntryDeclarationWithUnexpectedIdentifier()
        {
            var test = @"
namespace Foo {
machine M {
start state S
{
entry Bar {}
}
}
}";

            ParsingOptions options = ParsingOptions.CreateDefault()
                .DisableThrowParsingException();
            var parser = new PSharpParser(new PSharpProject(),
                SyntaxFactory.ParseSyntaxTree(test), options);

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual("Expected \"{\".",
                parser.GetParsingErrorLog());
        }

        [TestMethod, Timeout(10000)]
        public void TestOnEventGotoStateDeclarationWithoutSemicolon()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on e goto S2
}
}
}";

            ParsingOptions options = ParsingOptions.CreateDefault()
                .DisableThrowParsingException();
            var parser = new PSharpParser(new PSharpProject(),
                SyntaxFactory.ParseSyntaxTree(test), options);

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual("Expected \";\".",
                parser.GetParsingErrorLog());
        }

        [TestMethod, Timeout(10000)]
        public void TestOnEventGotoStateDeclarationWithoutState()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on e goto;
}
}
}";

            ParsingOptions options = ParsingOptions.CreateDefault()
                .DisableThrowParsingException();
            var parser = new PSharpParser(new PSharpProject(),
                SyntaxFactory.ParseSyntaxTree(test), options);

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual("Expected state identifier.",
                parser.GetParsingErrorLog());
        }

        [TestMethod, Timeout(10000)]
        public void TestOnEventDoActionDeclarationWithoutSemicolon()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on e do Bar
}
}
}";

            ParsingOptions options = ParsingOptions.CreateDefault()
                .DisableThrowParsingException();
            var parser = new PSharpParser(new PSharpProject(),
                SyntaxFactory.ParseSyntaxTree(test), options);

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual("Expected \";\".",
                parser.GetParsingErrorLog());
        }

        [TestMethod, Timeout(10000)]
        public void TestOnEventDoActionDeclarationWithoutAction()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on e do;
}
}
}";

            ParsingOptions options = ParsingOptions.CreateDefault()
                .DisableThrowParsingException();
            var parser = new PSharpParser(new PSharpProject(),
                SyntaxFactory.ParseSyntaxTree(test), options);

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual("Expected action identifier.",
                parser.GetParsingErrorLog());
        }

        [TestMethod, Timeout(10000)]
        public void TestOnEventDeclarationWithoutHandler()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on e;
}
}
}";

            ParsingOptions options = ParsingOptions.CreateDefault()
                .DisableThrowParsingException();
            var parser = new PSharpParser(new PSharpProject(),
                SyntaxFactory.ParseSyntaxTree(test), options);

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual("Expected \"do\", \"goto\" or \"push\".",
                parser.GetParsingErrorLog());
        }

        [TestMethod, Timeout(10000)]
        public void TestIgnoreEventDeclarationWithoutComma()
        {
            var test = @"
namespace Foo {
machine M {
start state S
{
ignore e1 e2;
}
}
}";

            ParsingOptions options = ParsingOptions.CreateDefault()
                .DisableThrowParsingException();
            var parser = new PSharpParser(new PSharpProject(),
                SyntaxFactory.ParseSyntaxTree(test), options);

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual("Expected \",\".",
                parser.GetParsingErrorLog());
        }

        [TestMethod, Timeout(10000)]
        public void TestDeferEventDeclarationWithoutComma()
        {
            var test = @"
namespace Foo {
machine M {
start state S
{
defer e1 e2;
}
}
}";

            ParsingOptions options = ParsingOptions.CreateDefault()
                .DisableThrowParsingException();
            var parser = new PSharpParser(new PSharpProject(),
                SyntaxFactory.ParseSyntaxTree(test), options);

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual("Expected \",\".",
                parser.GetParsingErrorLog());
        }
    }
}
