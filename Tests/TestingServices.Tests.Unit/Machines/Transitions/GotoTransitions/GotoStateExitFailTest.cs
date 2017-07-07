//-----------------------------------------------------------------------
// <copyright file="GotoStateExitFailTest.cs">
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

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
    public class GotoStateExitFailTest : BaseTest
    {
        class Program : Machine
        {
            [Start]
            [OnEntry(nameof(EntryInit))]
            [OnExit(nameof(ExitInit))]
            class Init : MachineState { }

            void EntryInit()
            {
                this.Goto(typeof(Done));
            }

            void ExitInit()
            {
                // This assert is reachable.
                this.Assert(false, "Bug found.");
            }
            
            class Done : MachineState { }
        }

        [Fact]
        public void TestGotoStateExitFail()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(Program));
            });

            base.AssertFailed(test, 1, true);
        }
    }
}
