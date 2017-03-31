//-----------------------------------------------------------------------
// <copyright file="MachineTests.cs">
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
using Microsoft.PSharp.LanguageServices.Parsing;

using Xunit;

namespace Microsoft.PSharp.LanguageServices.Tests.Unit
{
    public class MachineTests
    {
        #region correct tests

        [Fact]
        public void TestMachineDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
start state S { }
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Machine
    {
        [Microsoft.PSharp.Start]
        class S : MachineState
        {
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        #endregion

        #region failure tests

        [Fact]
        public void TestMachineDeclarationWithoutState()
        {
            var test = @"
namespace Foo {
machine M {
}
}";
            LanguageTestUtilities.AssertFailedTestLog("A machine must declare at least one state.", test);
        }

        [Fact]
        public void TestMachineDeclarationWithoutIdentifier()
        {
            var test = @"
namespace Foo {
machine{}
}";
            LanguageTestUtilities.AssertFailedTestLog("Expected machine identifier.", test);
        }

        [Fact]
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
            LanguageTestUtilities.AssertFailedTestLog("Unexpected token.", test);
        }

        [Fact]
        public void TestMachineDeclarationWithoutStartState()
        {
            var test = @"
namespace Foo {
machine M {
state S { }
}
}";
            LanguageTestUtilities.AssertFailedTestLog("A machine must declare a start state.", test);
        }

        [Fact]
        public void TestMachineDeclarationWithoutStartState2()
        {
            var test = "" +
                "namespace Foo {" +
                "machine M {" +
                "state S1 { }" +
                "state S2 { }" +
                "}" +
                "}";
            LanguageTestUtilities.AssertFailedTestLog("A machine must declare a start state.", test);
        }

        [Fact]
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
            LanguageTestUtilities.AssertFailedTestLog("A machine must declare a start state.", test);
        }

        [Fact]
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
            LanguageTestUtilities.AssertFailedTestLog("A machine can declare only a single start state.", test);
        }

        [Fact]
        public void TestPrivateMachineDeclaration()
        {
            var test = @"
namespace Foo {
private machine M { }
}";

            ParsingOptions options = ParsingOptions.CreateDefault()
                .DisableThrowParsingException();
            var parser = new PSharpParser(new PSharpProject(),
                SyntaxFactory.ParseSyntaxTree(test), options);

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.Equal("A machine cannot be declared as private.",
                parser.GetParsingErrorLog());
        }

        [Fact]
        public void TestProtectedMachineDeclaration()
        {
            var test = @"
namespace Foo {
protected machine M { }
}";

            ParsingOptions options = ParsingOptions.CreateDefault()
                .DisableThrowParsingException();
            var parser = new PSharpParser(new PSharpProject(),
                SyntaxFactory.ParseSyntaxTree(test), options);

            var tokens = new PSharpLexer().Tokenize(test);
            var program = parser.ParseTokens(tokens);

            Assert.Equal("A machine cannot be declared as protected.",
                parser.GetParsingErrorLog());
        }

        #endregion
    }
}
