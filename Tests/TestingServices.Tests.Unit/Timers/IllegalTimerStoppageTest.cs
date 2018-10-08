//-----------------------------------------------------------------------
// <copyright file="IllegalTimerStoppageTest.cs">
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
using System.Threading.Tasks;

using Microsoft.PSharp.Timers;

using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
    public class IllegalTimerStoppageTest : BaseTest
    {
        public IllegalTimerStoppageTest(ITestOutputHelper output)
            : base(output)
        { }
        private class TransferTimer : Event
        {
            public TimerId tid;

            public TransferTimer(TimerId tid)
            {
                this.tid = tid;
            }
        }
        private class T2 : TimedMachine
        {
            TimerId tid;
            object payload = new object();
            MachineId m;

            [Start]
            [OnEntry(nameof(Initialize))]
            [IgnoreEvents(typeof(TimerElapsedEvent))]
            class Init : MachineState { }

            void Initialize()
            {
                tid = this.StartTimer(this.payload, 100, true);
                m = CreateMachine(typeof(T3), new TransferTimer(tid));
                this.Raise(new Halt());
            }
        }

        private class T3 : TimedMachine
        {
            [Start]
            [OnEntry(nameof(Initialize))]
            class Init : MachineState { }

            async Task Initialize()
            {
                TimerId tid = (this.ReceivedEvent as TransferTimer).tid;

                // trying to stop a timer created by a different machine.
                // should throw an assertion violation
                await this.StopTimer(tid, true);
                this.Raise(new Halt());
            }
        }

        [Fact]
        public void IllegalTimerStopTest()
        {
            var config = Configuration.Create().WithNumberOfIterations(1000);
            config.MaxSchedulingSteps = 200;

            var test = new Action<IMachineRuntime>((r) => {
                r.CreateMachine(typeof(T2));
            });
            base.AssertFailed(test, 1, true);
        }
    }
}
