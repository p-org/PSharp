//-----------------------------------------------------------------------
// <copyright file="CurrentStateTest.cs">
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

using Microsoft.PSharp.Utilities;

using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
    public class CurrentStateTest : BaseTest
    {
        public CurrentStateTest(ITestOutputHelper output)
            : base(output)
        { }

        class Unit : Event { }

        class Server : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(Unit), typeof(Active))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.Assert(this.CurrentState == typeof(Init));
                this.Raise(new Unit());
            }

            [OnEntry(nameof(ActiveOnEntry))]
            class Active : MachineState { }

            void ActiveOnEntry()
            {
                this.Assert(this.CurrentState == typeof(Active));
            }
        }

        /// <summary>
        /// P# semantics test: current state must be of the expected type.
        /// </summary>
        [Fact]
        public void TestCurrentState()
        {
            var configuration = base.GetConfiguration();
            configuration.SchedulingStrategy = SchedulingStrategy.DFS;

            var test = new Action<IPSharpRuntime>((r) => {
                r.CreateMachine(typeof(Server));
            });

            base.AssertSucceeded(configuration, test);
        }
    }
}
