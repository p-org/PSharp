//-----------------------------------------------------------------------
// <copyright file="Logger.cs">
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
using BenchmarkDotNet.Attributes.Jobs;
using BenchmarkDotNet.Engines;
using System.IO;
using System;
using System.Reflection;
using System.Linq;

namespace Microsoft.PSharp.Core.Tests.Performance
{
    /// <summary>
    /// Warning: this test requires ~1.5 GB of free disk space to run
    /// It generates a log file in the bin directory
    /// In the interest of diagnostics, it chooses not to delete the generated log file
    /// </summary>
    [Config(typeof(Configuration))]
    [SimpleJob(RunStrategy.Monitoring)]
    public class LoggerTest
    {
        public partial class SimpleMachine
        {
            private PSharpRuntime instance;
            private TaskCompletionSource<bool> tcs;
            //private string dictName = "SomeDictionary";

            public SimpleMachine()
            {
                //this.Name = "SimpleMachine";
                this.Counter = 0;
                this.targetRounds = 0;
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
        }


        public class SetContextMessage : Event
        {
            public PSharpRuntime runtime;
            public int rounds;
            public TaskCompletionSource<bool> tcs;

            public SetContextMessage(PSharpRuntime runtime, int rounds, TaskCompletionSource<bool> tcs)
                : base()
            {
                this.runtime = runtime;
                this.rounds = rounds;
                this.tcs = tcs;
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
            private int targetRounds;
            private int rounds;

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
                var e = this.ReceivedEvent as SetContextMessage;
                this.instance = e.runtime;
                this.targetRounds = e.rounds;
                this.tcs = e.tcs;
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
                this.rounds++;

                if(this.rounds == this.targetRounds)
                {
                    this.Raise(new Halt());
                    this.tcs.SetResult(true);                                        
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

        [Params(100, 1000, 10000)]
        public int Clients { get; set; }

        [Params(8)]
        public int TargetRounds { get; set; }

        
        private string path;
        private string basePath;


        [GlobalSetup]
        public void GlobalSetup()
        {
            basePath = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            path = basePath + String.Format(@"\\LoggerTestlog.txt");
        }
              
        [Benchmark(Baseline = true)]
        public void RunSyncNull()
        {
            using (var logger = new SyncWriterLogger(null))
            {
                var configuration = PSharp.Configuration.Create().WithVerbosityEnabled(2);
                var runtime = new StateMachineRuntime(configuration);
                runtime.SetLogger(logger);
                ConcurrentQueue<MachineId> machines = new ConcurrentQueue<MachineId>();
                RunMachine(runtime, machines);
            }
        }

        [Benchmark]
        public void RunAsyncNull()
        {
            using (var logger = new AsyncLogger(null))
            {
                var configuration = PSharp.Configuration.Create().WithVerbosityEnabled(2);
                var runtime = new StateMachineRuntime(configuration);
                runtime.SetLogger(logger);
                ConcurrentQueue<MachineId> machines = new ConcurrentQueue<MachineId>();
                RunMachine(runtime, machines);
            }
        }

        [Benchmark]
        public void RunSync()
        {
            using (var writer = new StreamWriter(path, false))
            {
                using (var logger = new SyncWriterLogger(writer))
                {
                    var configuration = PSharp.Configuration.Create().WithVerbosityEnabled(2);
                    var runtime = new StateMachineRuntime(configuration);
                    runtime.SetLogger(logger);
                    ConcurrentQueue<MachineId> machines = new ConcurrentQueue<MachineId>();
                    RunMachine(runtime, machines);
                }
            }
        }

        [Benchmark]
        public void RunAsync()
        {
            using (var writer = new StreamWriter(path, false))
            {
                using (var logger = new AsyncLogger(writer))
                {
                    var configuration = PSharp.Configuration.Create().WithVerbosityEnabled(2);
                    var runtime = new StateMachineRuntime(configuration);
                    runtime.SetLogger(logger);
                    ConcurrentQueue<MachineId> machines = new ConcurrentQueue<MachineId>();
                    RunMachine(runtime, machines);
                }
            }
        }

        private void RunMachine(StateMachineRuntime runtime, ConcurrentQueue<MachineId> machines)
        {
            var tcsArray = InitializeArray<TaskCompletionSource<bool>>(Clients);
            Parallel.For(0, Clients, index =>
            {
                machines.Enqueue(runtime.CreateMachine(typeof(SimpleMachine), new SetContextMessage(runtime, TargetRounds, tcsArray[index])));
            });

            var tasks = from x in tcsArray select x.Task;
            Task.WhenAll(tasks).Wait();
        }

        T[] InitializeArray<T>(int length) where T : new()
        {
            T[] array = new T[length];
            for (int i = 0; i < length; ++i)
            {
                array[i] = new T();
            }

            return array;
        }
    }
}
