//-----------------------------------------------------------------------
// <copyright file="AmbiguousEventHandlerTest.cs">
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
    public class AmbiguousEventHandlerTest : BaseTest
    {
        public AmbiguousEventHandlerTest(ITestOutputHelper output)
            : base(output)
        { }

        class E : Event { }

        class Program : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(HandleE))]
            public class Init : MachineState { }

            void InitOnEntry()
            {
                this.Send(this.Id, new E());
            }

            void HandleE() { }
            void HandleE(int k) { }
        }

        class Safety : Monitor
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(HandleE))]
            public class Init : MonitorState { }

            void InitOnEntry()
            {
                this.Raise(new E());
            }

            void HandleE() { }
            void HandleE(int k) { }
        }

        [Fact]
        public void TestAmbiguousMachineEventHandler()
        {
            var test = new Action<IMachineRuntime>((r) => {
                r.CreateMachine(typeof(Program));
            });

            base.AssertSucceeded(test);
        }

        [Fact]
        public void TestAmbiguousMonitorEventHandler()
        {
            var test = new Action<IMachineRuntime>((r) => {
                r.RegisterMonitor(typeof(Safety));
            });

            base.AssertSucceeded(test);
        }
    }
}
