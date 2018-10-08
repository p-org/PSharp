//-----------------------------------------------------------------------
// <copyright file="SEMOneMachine20Test.cs">
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

namespace Microsoft.PSharp.TestingServices.Tests.Integration
{
    public class SEMOneMachine20Test : BaseTest
    {
        class E : Event { }

        class Real1 : Machine
        {
            [Start]
            [OnEntry(nameof(EntryInit))]
            [OnExit(nameof(ExitInit))]
            [OnEventPushState(typeof(E), typeof(Call))]
            class Init : MachineState { }

            void EntryInit()
            {
                this.Raise(new E());
            }

            void ExitInit() { }

            [OnEntry(nameof(EntryCall))]
            [OnExit(nameof(ExitCall))]
            class Call : MachineState { }

            void EntryCall()
            {
                this.Pop();
            }

            void ExitCall()
            {
                this.Assert(false);
            }
        }

        /// <summary>
        /// Exit function performed while explicitly popping the state.
        /// </summary>
        [Fact]
        public void TestExitAtExplicitPop()
        {
            var test = new Action<IMachineRuntime>((r) => {
                r.CreateMachine(typeof(Real1));
            });

            base.AssertFailed(test, 1);
        }
    }
}
