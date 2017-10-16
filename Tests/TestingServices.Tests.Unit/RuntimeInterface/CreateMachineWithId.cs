//-----------------------------------------------------------------------
// <copyright file="CreateMachineWithId.cs">
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

using Xunit;

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
    public class CreateMachineWithId : BaseTest
    {
        class E : Event { }

        class LivenessMonitor : Monitor
        {
            [Start]
            [Hot]
            [OnEventGotoState(typeof(E), typeof(S2))]
            class S1 : MonitorState { }

            [Hot]
            [OnEventGotoState(typeof(E), typeof(S3))]
            class S2 : MonitorState { }

            [Cold]
            class S3 : MonitorState { }
        }

        class M : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.Monitor(typeof(LivenessMonitor), new E());
            }
        }

        [Fact]
        public void TestCreateWithId1()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.RegisterMonitor(typeof(LivenessMonitor));
                var m = r.CreateMachine(typeof(M));
                var mprime = r.IncGenerationForTesting(m);
                r.Assert(m != mprime);
                r.CreateMachine(mprime, typeof(M));
            });

            base.AssertSucceeded(test);
        }

        class Data
        {
            public int x;

            public Data()
            {
                x = 0;
            }
        }

        class E1 : Event
        {
            public Data data;

            public E1(Data data)
            {
                this.data = data;
            }
        }

        class TerminateReq : Event
        {
            public MachineId sender;
            public TerminateReq(MachineId sender)
            {
                this.sender = sender;
            }
        }

        class TerminateResp : Event { }

        class M1 : Machine
        {
            Data data;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(Process))]
            [OnEventDoAction(typeof(TerminateReq), nameof(Terminate))]
            class S : MachineState { }

            void InitOnEntry()
            {
                data = (this.ReceivedEvent as E1).data;
                Process();
            }

            void Process()
            {
                if(data.x != 10)
                {
                    data.x++;
                    this.Send(this.Id, new E());
                }
                else
                {
                    this.Monitor(typeof(LivenessMonitor), new E());
                    this.Monitor(typeof(LivenessMonitor), new E());
                }

            }

            void Terminate()
            {
                this.Send((this.ReceivedEvent as TerminateReq).sender, new TerminateResp());
                this.Raise(new Halt());
            }
        }

        class Harness : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class S : MachineState { }

            void InitOnEntry()
            {
                var data = new Data();
                var m1 = this.CreateMachine(typeof(M1), new E1(data));
                var m2 = this.Id.Runtime.IncGenerationForTesting(m1);
                this.Send(m1, new TerminateReq(this.Id));
                this.Receive(typeof(TerminateResp));
                this.Id.Runtime.CreateMachine(m2, typeof(M1), new E1(data));
            }
        }

        [Fact]
        public void TestCreateWithId2()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.RegisterMonitor(typeof(LivenessMonitor));
                var m = r.CreateMachine(typeof(Harness));
            });

            base.AssertSucceeded(test);
        }
    }
}
