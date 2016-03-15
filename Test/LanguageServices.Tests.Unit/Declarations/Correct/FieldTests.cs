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

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Microsoft.PSharp.LanguageServices.Parsing;

namespace Microsoft.PSharp.LanguageServices.Tests.Unit
{
    [TestClass]
    public class FieldTests
    {
        [TestMethod, Timeout(3000)]
        public void TestIntFieldDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
int k;
start state S { }
}
}";

            var tokens = new PSharpLexer().Tokenize(test);
            var program = new PSharpParser(new PSharpProject(),
                SyntaxFactory.ParseSyntaxTree(test), false).ParseTokens(tokens);
            program.Rewrite();

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
                program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(3000)]
        public void TestMachineFieldDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
machine N;
start state S { }
}
}";

            var tokens = new PSharpLexer().Tokenize(test);
            var program = new PSharpParser(new PSharpProject(),
                SyntaxFactory.ParseSyntaxTree(test), false).ParseTokens(tokens);
            program.Rewrite();

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
                program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }

        [TestMethod, Timeout(3000)]
        public void TestListFieldDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
List<int> k;
start state S { }
}
}";

            var tokens = new PSharpLexer().Tokenize(test);
            var program = new PSharpParser(new PSharpProject(),
                SyntaxFactory.ParseSyntaxTree(test), false).ParseTokens(tokens);
            program.Rewrite();

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
                program.GetSyntaxTree().ToString().Replace("\n", string.Empty));
        }
    }
}
