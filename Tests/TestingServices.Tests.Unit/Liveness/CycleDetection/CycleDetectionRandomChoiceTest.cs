//-----------------------------------------------------------------------
// <copyright file="CycleDetectionRandomChoiceTest.cs">
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
    public class CycleDetectionRandomChoiceTest : BaseTest
    {
        class Configure: Event
        {
            public bool ApplyFix;

            public Configure(bool applyFix)
            {
                this.ApplyFix = applyFix;
            }
        }

        class Message : Event { }

        class EventHandler : Machine
        {
            bool ApplyFix;

            [Start]
            [OnEntry(nameof(OnInitEntry))]
            [OnEventDoAction(typeof(Message), nameof(OnMessage))]
            class Init : MachineState { }

            void OnInitEntry()
            {
                this.ApplyFix = (this.ReceivedEvent as Configure).ApplyFix;
                this.Send(this.Id, new Message());
            }

            void OnMessage()
            {
                this.Send(this.Id, new Message());
                this.Monitor<WatchDog>(new WatchDog.NotifyMessage());
                if (this.Choose())
                {
                    this.Monitor<WatchDog>(new WatchDog.NotifyDone());
                    this.Raise(new Halt());
                }
            }

            bool Choose()
            {
                if (this.ApplyFix)
                {
                    return this.FairRandom();
                }
                else
                {
                    return this.Random();
                }
            }
        }

        class WatchDog : Monitor
        {
            public class NotifyMessage : Event { }
            public class NotifyDone : Event { }

            [Start]
            [Hot]
            [OnEventGotoState(typeof(NotifyMessage), typeof(HotState))]
            [OnEventGotoState(typeof(NotifyDone), typeof(ColdState))]
            class HotState : MonitorState { }

            [Cold]
            class ColdState : MonitorState { }
        }

        [Fact]
        public void TestCycleDetectionRandomChoiceNoBug()
        {
            var configuration = base.GetConfiguration();
            configuration.EnableProgramStateCaching = true;
            configuration.EnableCycleReplaying = true;
            configuration.RandomSchedulingSeed = 906;
            configuration.SchedulingIterations = 7;
            configuration.MaxSchedulingSteps = 200;

            var test = new Action<PSharpRuntime>((r) => {
                r.RegisterMonitor(typeof(WatchDog));
                r.CreateMachine(typeof(EventHandler), new Configure(true));
            });

            base.AssertSucceeded(configuration, test);
        }

        [Fact]
        public void TestCycleDetectionRandomChoiceBug()
        {
            var configuration = base.GetConfiguration();
            configuration.EnableProgramStateCaching = true;
            configuration.EnableCycleReplaying = true;
            configuration.RandomSchedulingSeed = 906;
            configuration.SchedulingIterations = 10;
            configuration.MaxSchedulingSteps = 200;

            var test = new Action<PSharpRuntime>((r) => {
                r.RegisterMonitor(typeof(WatchDog));
                r.CreateMachine(typeof(EventHandler), new Configure(false));
            });

            string bugReport = "Monitor 'WatchDog' detected infinite execution that violates a liveness property.";
            base.AssertFailed(configuration, test, bugReport);
        }
    }
}
