//-----------------------------------------------------------------------
// <copyright file="MachineFailTests.cs">
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
    public class MachineFailTests
    {
        [TestMethod, Timeout(3000)]
        public void TestMachineDeclarationWithoutState()
        {
            var test = @"
namespace Foo {
machine M {
}
}";

            var parser = new PSharpParser(new PSharpProject(),
                SyntaxFactory.ParseSyntaxTree(test), false);

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual("A machine must declare at least one state.",
                parser.GetParsingErrorLog());
        }

        [TestMethod, Timeout(3000)]
        public void TestMachineDeclarationWithoutIdentifier()
        {
            var test = @"
namespace Foo {
machine{}
}";

            var parser = new PSharpParser(new PSharpProject(),
                SyntaxFactory.ParseSyntaxTree(test), false);

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual("Expected machine identifier.",
                parser.GetParsingErrorLog());
        }

        [TestMethod, Timeout(3000)]
        public void TestMachineDeclarationWithTwoBodies()
        {
            var test = @"
namespace Foo {
machine M {
start state S { }
}
{
}
}";

            var parser = new PSharpParser(new PSharpProject(),
                SyntaxFactory.ParseSyntaxTree(test), false);

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual("Unexpected token.",
                parser.GetParsingErrorLog());
        }

        [TestMethod, Timeout(3000)]
        public void TestMachineDeclarationWithoutStartState()
        {
            var test = @"
namespace Foo {
machine M {
state S { }
}
}";

            var parser = new PSharpParser(new PSharpProject(),
                SyntaxFactory.ParseSyntaxTree(test), false);

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual("A machine must declare a start state.",
                parser.GetParsingErrorLog());
        }

        [TestMethod, Timeout(3000)]
        public void TestMachineDeclarationWithoutStartState2()
        {
            var test = "" +
                "namespace Foo {" +
                "machine M {" +
                "state S1 { }" +
                "state S2 { }" +
                "}" +
                "}";

            var parser = new PSharpParser(new PSharpProject(),
                SyntaxFactory.ParseSyntaxTree(test), false);

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual("A machine must declare a start state.",
                parser.GetParsingErrorLog());
        }

        [TestMethod, Timeout(3000)]
        public void TestMachineDeclarationWithoutStartState3()
        {
            var test = @"
namespace Foo {
machine M {
state S1 { }
state S2 {}

state S3 { }
}
}";

            var parser = new PSharpParser(new PSharpProject(),
                SyntaxFactory.ParseSyntaxTree(test), false);

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual("A machine must declare a start state.",
                parser.GetParsingErrorLog());
        }

        [TestMethod, Timeout(3000)]
        public void TestMachineDeclarationWithMoreThanOneStartState()
        {
            var test = @"
namespace Foo {
machine M {
start state S1 {}
start state S2 { }

start state S3 { }
}
}";

            var parser = new PSharpParser(new PSharpProject(),
                SyntaxFactory.ParseSyntaxTree(test), false);

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.AreEqual("A machine can declare only a single start state.",
                parser.GetParsingErrorLog());
        }
    }
}
