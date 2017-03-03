//-----------------------------------------------------------------------
// <copyright file="EventFailTests.cs">
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
    public class EventFailTests
    {
        [TestMethod, Timeout(10000)]
        public void TestProtectedEventDeclaration()
        {
            var test = @"
namespace Foo {
protected event e;
}";

            ParsingOptions options = ParsingOptions.CreateDefault()
                .DisableThrowParsingException();
            var parser = new PSharpParser(new PSharpProject(),
                SyntaxFactory.ParseSyntaxTree(test), options);

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual("Event and machine declarations must be internal or public.",
                parser.GetParsingErrorLog());
        }

        [TestMethod, Timeout(10000)]
        public void TestPrivateEventDeclaration()
        {
            var test = @"
namespace Foo {
private event e;
}";

            ParsingOptions options = ParsingOptions.CreateDefault()
                .DisableThrowParsingException();
            var parser = new PSharpParser(new PSharpProject(),
                SyntaxFactory.ParseSyntaxTree(test), options);

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual(parser.GetParsingErrorLog(),
                "Event and machine declarations must be internal or public.");
        }

        [TestMethod, Timeout(10000)]
        public void TestEventDeclarationWithoutNamespace()
        {
            var test = "event e;";

            ParsingOptions options = ParsingOptions.CreateDefault()
                .DisableThrowParsingException();
            var parser = new PSharpParser(new PSharpProject(),
                SyntaxFactory.ParseSyntaxTree(test), options);

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual("Must be declared inside a namespace.",
                parser.GetParsingErrorLog());
        }

        [TestMethod, Timeout(10000)]
        public void TestEventDeclarationWithGenericError1()
        {
            var test = @"
namespace Foo {
event e>;
}";

            ParsingOptions options = ParsingOptions.CreateDefault()
                .DisableThrowParsingException();
            var parser = new PSharpParser(new PSharpProject(),
                SyntaxFactory.ParseSyntaxTree(test), options);

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual("Expected \"(\" or \";\".",
                parser.GetParsingErrorLog());
        }

        [TestMethod, Timeout(10000)]
        public void TestEventDeclarationWithGenericError2()
        {
            var test = @"
namespace Foo {
event e<;
}";

            ParsingOptions options = ParsingOptions.CreateDefault()
                .DisableThrowParsingException();
            var parser = new PSharpParser(new PSharpProject(),
                SyntaxFactory.ParseSyntaxTree(test), options);

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual("Invalid generic expression.",
                parser.GetParsingErrorLog());
        }

        [TestMethod, Timeout(10000)]
        public void TestNonPublicOrInternalEventDeclarationInMachine()
        {
            var test = @"
namespace Foo {
machine M {
event e;
}
{
}
}";

            ParsingOptions options = ParsingOptions.CreateDefault()
                .DisableThrowParsingException();
            var parser = new PSharpParser(new PSharpProject(),
                SyntaxFactory.ParseSyntaxTree(test), options);

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual("An event declared in the scope of a machine must be public or internal.",
                parser.GetParsingErrorLog());
        }

        [TestMethod, Timeout(10000)]
        public void TestProtectedEventDeclarationInMachine()
        {
            var test = @"
namespace Foo {
machine M {
protected event e;
}
{
}
}";

            ParsingOptions options = ParsingOptions.CreateDefault()
                .DisableThrowParsingException();
            var parser = new PSharpParser(new PSharpProject(),
                SyntaxFactory.ParseSyntaxTree(test), options);

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual("An event cannot be protected.",
                parser.GetParsingErrorLog());
        }

        [TestMethod, Timeout(10000)]
        public void TestPrivateEventDeclarationInMachine()
        {
            var test = @"
namespace Foo {
machine M {
private event e;
}
{
}
}";

            ParsingOptions options = ParsingOptions.CreateDefault()
                .DisableThrowParsingException();
            var parser = new PSharpParser(new PSharpProject(),
                SyntaxFactory.ParseSyntaxTree(test), options);

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual("An event cannot be private.",
                parser.GetParsingErrorLog());
        }
    }
}
