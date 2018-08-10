//-----------------------------------------------------------------------
// <copyright file="TimerLivenessTest.cs">
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

using Microsoft.PSharp.Timers;

using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
    public class TimerLivenessTest : BaseTest
    {
        public TimerLivenessTest(ITestOutputHelper output)
            : base(output)
        { }

        class TimeoutReceivedEvent : Event { }

        class Client : TimedMachine
        {
            TimerId tid;
            object payload = new object();

            [Start]
            [OnEntry(nameof(Initialize))]
            [OnEventDoAction(typeof(TimerElapsedEvent), nameof(HandleTimeout))]
            private class Init : MachineState { }

            private void Initialize()
            {
                tid = StartTimer(payload, 10, false);
            }

            private void HandleTimeout()
            {
                this.Monitor<LivenessMonitor>(new TimeoutReceivedEvent());
            }
        }

        class LivenessMonitor : Monitor
        {
            [Start]
            [Hot]
            [OnEventGotoState(typeof(TimeoutReceivedEvent), typeof(TimeoutReceived))]
            class NoTimeoutReceived : MonitorState { }

            [Cold]
            class TimeoutReceived : MonitorState { }
        }

        [Fact]
        public void PeriodicLivenessTest()
        {
            var config = base.GetConfiguration();
            config.LivenessTemperatureThreshold = 150;
            config.MaxSchedulingSteps = 300;
            config.SchedulingIterations = 1000;

            var test = new Action<IPSharpRuntime>((r) => {
                r.RegisterMonitor(typeof(LivenessMonitor));
                r.CreateMachine(typeof(Client));
            });

            base.AssertSucceeded(config, test);
        }
    }
}
