//-----------------------------------------------------------------------
// <copyright file="CreateMachinesTest.cs">
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

using System.Threading;
using System.Threading.Tasks;

using BenchmarkDotNet.Attributes;
using System;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Attributes.Jobs;

namespace Microsoft.PSharp.Core.Tests.Performance
{
    /// <summary>
    /// Tests P# performance when creating a lot of machines
    /// Here, every machine creates 2 child machines and so on
    /// Creates 2^x - 1 machines, where x is the count passed into Spreader.Config
    /// This benchmark is adapted from https://github.com/ponylang/ponyc/tree/master/examples/spreader
    /// </summary>
    [Config(typeof(Configuration))]
    [SimpleJob(RunStrategy.Monitoring)]
    public class TaskMachineTest
    {

        internal class SingleTaskMachineEvent : Event
        {
            /// <summary>
            /// Payload
            /// </summary>
            public Func<SingleTaskMachine, Task> function;

            /// <summary>
            /// Constructor
            /// </summary>
            public SingleTaskMachineEvent(Func<SingleTaskMachine, Task> function)
            {
                this.function = function;
            }

        }

        internal class FastSingleTaskMachineEvent : Event
        {
            /// <summary>
            /// Payload
            /// </summary>
            public Func<FastSingleTaskMachine, Task> function;

            /// <summary>
            /// Constructor
            /// </summary>
            public FastSingleTaskMachineEvent(Func<FastSingleTaskMachine, Task> function)
            {
                this.function = function;
            }

        }


        internal class SingleTaskMachine : Machine
        {
            [Start]
            [OnEntry(nameof(Run))]
            class InitState : MachineState { }

            /// <summary>
            /// Executes the payload
            /// </summary>
            async Task Run()
            {
                var function = (this.ReceivedEvent as SingleTaskMachineEvent).function;
                await function(this);
                this.Raise(new Halt());
            }

            /// <summary>
            /// Public Send
            /// </summary>
            public void MySend(MachineId target, Event e)
            {
                this.Send(target, e);
            }

        }

        [Fast]
        internal class FastSingleTaskMachine : Machine
        {
            [Start]
            [OnEntry(nameof(Run))]
            class InitState : MachineState { }

            /// <summary>
            /// Executes the payload
            /// </summary>
            async Task Run()
            {
                var function = (this.ReceivedEvent as FastSingleTaskMachineEvent).function;
                await function(this);
                this.Raise(new Halt());
            }

            /// <summary>
            /// Public Send
            /// </summary>
            public void MySend(MachineId target, Event e)
            {
                this.Send(target, e);
            }

        }


        class ConfigureMachine : Event
        {
            public TaskCompletionSource<bool> tcs;
            public long numberOfMessages;

            public ConfigureMachine(TaskCompletionSource<bool> tcs, long numberOfMessages)
            {
                this.tcs = tcs;
                this.numberOfMessages = numberOfMessages;
            }
        }

        class E : Event { }

        class M : Machine
        {
            TaskCompletionSource<bool> tcs;
            long counter;
            long numberOfMessages;

            [Start]
            [OnEntry(nameof(Cons))]
            [OnEventDoAction(typeof(E), nameof(Inc))]
            class Init : MachineState { }

            void Cons()
            {
                this.tcs = (this.ReceivedEvent as ConfigureMachine).tcs;
                this.numberOfMessages = (this.ReceivedEvent as ConfigureMachine).numberOfMessages;
                this.counter = 0;
            }

            void Inc()
            {
                counter++;
                if (counter == this.numberOfMessages)
                {
                    tcs.SetResult(true);
                }
            }
        }

        [Fast]
        class FM : Machine
        {
            TaskCompletionSource<bool> tcs;
            long counter;
            private long numberOfMessages;

            [Start]
            [OnEntry(nameof(Cons))]
            [OnEventDoAction(typeof(E), nameof(Inc))]
            class Init : MachineState { }

            void Cons()
            {
                this.tcs = (this.ReceivedEvent as ConfigureMachine).tcs;
                this.numberOfMessages = (this.ReceivedEvent as ConfigureMachine).numberOfMessages;
                this.counter = 0;
            }

            void Inc()
            {
                counter++;
                if (counter == this.numberOfMessages)
                {
                    tcs.SetResult(true);
                }
            }
        }


        [Params(10000, 100000)]
        public int NumberOfMessages { get; set; }

        private PSharpRuntime runtime;
        MachineId supervisor, supervisorFast;
        private TaskCompletionSource<bool> tcs, tcsFast;

        [GlobalSetup]
        public void GlobalSetup()
        {
            runtime = PSharpRuntime.Create();
            tcs = new TaskCompletionSource<bool>();
            tcsFast = new TaskCompletionSource<bool>();
            supervisor = runtime.CreateMachine(typeof(M), new ConfigureMachine(tcs, NumberOfMessages));
            supervisorFast = runtime.CreateMachine(typeof(FM), new ConfigureMachine(tcsFast, NumberOfMessages));
        }

        [Benchmark(Baseline = true)]
        public void TaskRun()
        {
            for (int i = 0; i < NumberOfMessages; i++)
            {
                Task.Run(async () =>
                {
                    await Task.Yield();
                    runtime.SendEvent(supervisor, new E());
                });
            }
        }

        [Benchmark]
        public void FastSingleTaskRun()
        {
            for (int i = 0; i < NumberOfMessages; i++)
            {
                runtime.CreateMachine(typeof(FastSingleTaskMachine), new FastSingleTaskMachineEvent(
                async (v) =>
                {
                    await Task.Yield();
                    v.MySend(supervisorFast, new E());
                }));
            }
        }

        [Benchmark]
        public void TaskRunF()
        {
            for (int i = 0; i < NumberOfMessages; i++)
            {
                Task.Run(async () =>
                {
                    await Task.Yield();
                    runtime.SendEvent(supervisorFast, new E());
                });
            }
        }

        [Benchmark]
        public void SingleTaskRun()
        {
            for (int i = 0; i < NumberOfMessages; i++)
            {
                runtime.CreateMachine(typeof(SingleTaskMachine), new SingleTaskMachineEvent(
                async (v) =>
                {
                    await Task.Yield();
                    v.MySend(supervisor, new E());
                }));
            }

        }

        

    }
}
