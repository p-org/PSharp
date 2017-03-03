//-----------------------------------------------------------------------
// <copyright file="MethodTests.cs">
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
    public class MethodTests
    {
        [TestMethod, Timeout(10000)]
        public void TestVoidMethodDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
start state S { }
void Bar() { }
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

        void Bar()
        { }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [TestMethod, Timeout(10000)]
        public void TestVoidMethodDeclaration2()
        {
            var test = @"
namespace Foo {
machine M { start state S { } void Bar() { } }
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

        void Bar()
        { }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }
    }
}
