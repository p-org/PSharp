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

using Xunit;

namespace Microsoft.PSharp.LanguageServices.Tests.Unit
{
    public class MethodTests
    {
        #region correct tests

        [Fact]
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

        [Fact]
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

        #endregion

        #region failure tests

        [Fact]
        public void TestPublicMethodDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
start state S { }
public void Foo() { }
}
}";
            LanguageTestUtilities.AssertFailedTestLog("A machine method cannot be public.", test);
        }

        [Fact]
        public void TestInternalMethodDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
start state S { }
internal void Foo() { }
}
}";
            LanguageTestUtilities.AssertFailedTestLog("A machine method cannot be internal.", test);
        }

        [Fact]
        public void TestMethodDeclarationWithoutBrackets()
        {
            var test = @"
namespace Foo {
machine M {
start state S { }
void Foo()
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Expected \"{\" or \";\".", test);
        }

        #endregion
    }
}
