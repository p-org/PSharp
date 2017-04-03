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

using Xunit;

namespace Microsoft.PSharp.LanguageServices.Tests.Unit
{
    public class FieldTests
    {
        #region correct tests

        [Fact]
        public void TestIntFieldDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
int k;
start state S { }
}
}";
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
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestListFieldDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
List<int> k;
start state S { }
}
}";

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
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestMachineFieldDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
machine N;
start state S { }
}
}";
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
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestMachineArrayFieldDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
machine[] MachineArray;
List<machine> MachineList; 
List<machine[]> MachineArrayList; 
start state S { }
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Machine
    {
        MachineId[] MachineArray;
        List<MachineId> MachineList;
        List<MachineId[]> MachineArrayList;

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
        public void TestPublicFieldDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
public int k;
start state S { }
}
}";
            LanguageTestUtilities.AssertFailedTestLog("A machine field cannot be public.", test);
        }

        [Fact]
        public void TestInternalFieldDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
internal int k;
start state S { }
}
}";
            LanguageTestUtilities.AssertFailedTestLog("A machine field cannot be internal.", test);
        }

        [Fact]
        public void TestIntFieldDeclarationWithoutSemicolon()
        {
            var test = @"
namespace Foo {
machine M {
int k
start state S { }
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Expected \"(\" or \";\".", test);
        }

        [Fact]
        public void TestMachineFieldDeclarationWithoutSemicolon()
        {
            var test = @"
namespace Foo {
machine M {
machine N
start state S { }
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Expected \"(\" or \";\".", test);
        }

        [Fact]
        public void TestPrivateMachineFieldDeclarationWithoutSemicolon()
        {
            var test = @"
namespace Foo {
machine M {
private machine N
start state S { }
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Expected \"(\" or \";\".", test);
        }

        #endregion
    }
}
