//-----------------------------------------------------------------------
// <copyright file="LoggerTest.cs">
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

using System.Threading.Tasks;

using BenchmarkDotNet.Attributes;
using System.Collections.Concurrent;
using Microsoft.PSharp.IO;

namespace Microsoft.PSharp.Core.Tests.Performance
{
    [Config(typeof(Configuration))]
    public class LoggerTest
    {
        public partial class SimpleMachine
        {
            private PSharpRuntime instance;
            
            public SimpleMachine()
            {                
                this.Counter = 0;
            }

            public void SendMessageToPSharp()
            {

                Task.Factory.StartNew(async () =>
                {
                    await Task.Delay(0);
                    this.instance.SendEvent(this.Id, new Timeout());
                });
            }

            public void PersistInline()
            {

            }
        }


        public class SetContextMessage : Event
        {
            public PSharpRuntime runtime;

            public SetContextMessage(PSharpRuntime runtime)
                : base()
            {
                this.runtime = runtime;
            }
        }

        public partial class SimpleMachine : Machine
        {
            internal class Timeout : Event
            {
                public Timeout()
                    : base()
                {
                }
            }

            //ISimpleMachineContext context;
            //string Name;
            int Counter;

            [Microsoft.PSharp.Start]
            [OnEntry(nameof(InitOnEntry))]
            class Unknown : MachineState
            {
            }

            [OnEntry("psharp_SimpleThinking_on_entry_action")]
            [OnEventDoAction(typeof(SimpleMachine.Timeout), "IncrementCounter")]
            class SimpleThinking : MachineState
            {
            }

            [OnEntry("psharp_AdvancedThinking_on_entry_action")]
            [OnEventDoAction(typeof(SimpleMachine.Timeout), "IncrementCounter")]
            class AdvancedThinking : MachineState
            {
            }

            void InitOnEntry()
            {
                this.Counter = 0;
                this.instance = (this.ReceivedEvent as SetContextMessage).runtime;
                this.SendMessageToPSharp();
                this.Goto<SimpleThinking>();
            }

            void IncrementCounter()
            {
                this.Counter++;

                this.SendMessageToPSharp();

                if (this.Counter == 10)
                {
                    this.Goto<AdvancedThinking>();
                }

                if (this.Counter == 20)
                {
                    this.Counter = 0;
                    this.Goto<SimpleThinking>();
                }
            }

            protected void psharp_SimpleThinking_on_entry_action()
            {
                if (this.Counter == 0)
                {
                    for (int i = 0; i < 500; i++) ;
                }

                this.SendMessageToPSharp();
            }

            protected void psharp_AdvancedThinking_on_entry_action()
            {
                if (this.Counter == 10)
                {
                    this.PersistInline();
                }

                this.SendMessageToPSharp();
            }
        }

        [Params(1, 5, 10)]
        public int Clients { get; set; }

        [Benchmark]
        public void RunWithLogger()
        {
            var configuration = PSharp.Configuration.Create().WithVerbosityEnabled(0);
            var runtime = new StateMachineRuntime(configuration);            
            ConcurrentQueue<MachineId> machines = new ConcurrentQueue<MachineId>();
            Parallel.For(0, Clients, index =>
            {
                machines.Enqueue(runtime.CreateMachine(typeof(SimpleMachine), new SetContextMessage(runtime)));
            });

            foreach (var machine in machines)
            {
                runtime.SendEvent(machine, new Halt());
            }
        }        
    }
}
