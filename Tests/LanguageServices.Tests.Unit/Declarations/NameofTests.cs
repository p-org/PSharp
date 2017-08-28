//-----------------------------------------------------------------------
// <copyright file="NameofTests.cs">
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
    public class NameofTests
    {
        string NameofTest = @"
namespace Foo {
    machine M_with_nameof {
        start state Init
        {
            entry
            {
                Value += 1;
                raise(E1);
            }
            exit { Value += 10; }
            on E1 goto Next with { Value += 100; }
        }
        
        state Next
        {
            entry
            {
                Value += 1000;
                raise(E2);
            }
            on E2 do { Value += 10000; } 
        }
    }
}";

        [Fact]
        public void TestAllNameofRewriteWithNameof()
        {
            var test = NameofTest;
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M_with_nameof : Machine
    {
        [Microsoft.PSharp.Start]
        [OnEntry(nameof(psharp_Init_on_entry_action))]
        [OnExit(nameof(psharp_Init_on_exit_action))]
        [OnEventGotoState(typeof(E1), typeof(Next), nameof(psharp_Init_E1_action))]
        class Init : MachineState
        {
        }

        [OnEntry(nameof(psharp_Next_on_entry_action))]
        [OnEventDoAction(typeof(E2), nameof(psharp_Next_E2_action))]
        class Next : MachineState
        {
        }

        protected void psharp_Init_on_entry_action()
        {
            Value += 1;
            this.Raise(new E1());
        }

        protected void psharp_Init_on_exit_action()
        { Value += 10; }

        protected void psharp_Next_on_entry_action()
        {
            Value += 1000;
            this.Raise(new E2());
        }

        protected void psharp_Init_E1_action()
        { Value += 100; }

        protected void psharp_Next_E2_action()
        { Value += 10000; }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestAllNameofRewriteWithoutNameof()
        {
            var test = NameofTest;
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M_with_nameof : Machine
    {
        [Microsoft.PSharp.Start]
        [OnEntry(""psharp_Init_on_entry_action"")]
        [OnExit(""psharp_Init_on_exit_action"")]
        [OnEventGotoState(typeof(E1), typeof(Next), ""psharp_Init_E1_action"")]
        class Init : MachineState
        {
        }

        [OnEntry(""psharp_Next_on_entry_action"")]
        [OnEventDoAction(typeof(E2), ""psharp_Next_E2_action"")]
        class Next : MachineState
        {
        }

        protected void psharp_Init_on_entry_action()
        {
            Value += 1;
            this.Raise(new E1());
        }

        protected void psharp_Init_on_exit_action()
        { Value += 10; }

        protected void psharp_Next_on_entry_action()
        {
            Value += 1000;
            this.Raise(new E2());
        }

        protected void psharp_Init_E1_action()
        { Value += 100; }

        protected void psharp_Next_E2_action()
        { Value += 10000; }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test, csVersion: new System.Version(5, 0));
        }
    }
}
