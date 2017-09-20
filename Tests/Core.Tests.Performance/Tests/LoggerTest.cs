//-----------------------------------------------------------------------
// <copyright file="MailboxTest.cs">
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

            private PSharpRuntime instance;
            //private string dictName = "SomeDictionary";

            public SimpleMachine()
            {
                this.Name = "SimpleMachine";
                this.Counter = 0;
                int x = Name.Length;
            }

            public void SendMessageToPSharp()
            {

                Task.Factory.StartNew(async () =>
                {
                    await Task.Delay(0/*TimeSpan.FromSeconds(10)*/);
                    this.instance.SendEvent(this.Id, new Timeout());
                });
            }

            public void PersistInline()
            {

            }

            internal class Timeout : Event
            {
                public Timeout()
                    : base()
                {
                }
            }

            //ISimpleMachineContext context;
            string Name;
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
                    // Print to screen
                    //context.EventSource.Message("{0}: Inside Advance Thinking", this.Name);

                    // Persist
                    this.PersistInline();
                }

                this.SendMessageToPSharp();
            }
        }

        [Params(1, 5, 10)]
        public int Clients { get; set; }

        [Benchmark(Baseline = true)]
        public void RunWithSyncLogger()
        {
            var configuration = PSharp.Configuration.Create().WithVerbosityEnabled(2);
            var runtime = PSharpRuntime.Create(configuration);
            runtime.SetLogger(new SyncWriterLogger());
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

        [Benchmark]
        public void RunWithAsyncLogger()
        {
            var configuration = PSharp.Configuration.Create().WithVerbosityEnabled(2);
            var runtime = PSharpRuntime.Create(configuration);
            runtime.SetLogger(new AsyncLogger(null));
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

        //[Benchmark(Baseline = true)]
        //public void CreateTasks()
        //{
        //    Task[] tasks = new Task[Size];
        //    for (int idx = 0; idx < Size; idx++)
        //    {
        //        var task = new Task(() => { return; });
        //        task.Start();
        //        tasks[idx] = task;
        //    }

        //    Task.WaitAll(tasks);
        //}
    }
}
