//-----------------------------------------------------------------------
// <copyright file="GotoStateFailTest.cs">
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

using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
    public class GotoStateFailTest : BaseTest
    {
        public GotoStateFailTest(ITestOutputHelper output)
            : base(output)
        { }

        class Program : Machine
        {
            [Start]
            [OnEntry(nameof(EntryInit))]
            class Init : MachineState { }

            void EntryInit()
            {
                // This line no longer builds after converting from Goto(typeof(T)) to Goto<T>() 
                // due to the "where T: MachineState" constraint on Goto<T>().
                //this.Goto<object>();

                // Added a different failure mode here; try to Goto a state from another machine.
                this.Goto<Program2.Done>();
            }

            class Done : MachineState { }
        }

        class Program2 : Machine
        {
            [Start]
            [OnEntry(nameof(EntryInit))]
            class Init : MachineState { }

            void EntryInit()
            {
            }

            internal class Done : MachineState { }
        }

        [Fact]
        public void TestGotoStateFail()
        {
            var test = new Action<IPSharpRuntime>((r) => {
                r.CreateMachine(typeof(Program));
            });

            base.AssertFailed(test, 1, true);
        }
    }
}
