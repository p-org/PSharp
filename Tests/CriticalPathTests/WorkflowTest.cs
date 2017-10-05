using Microsoft.PSharp;
using System;
using System.Threading.Tasks;
using Xunit;

namespace CriticalPathTests
{
    public class WorkflowTest
    {
        internal class Configure : Event
        {
            public TaskCompletionSource<bool> TCS;

            public Configure(TaskCompletionSource<bool> tcs)
            {
                this.TCS = tcs;
            }
        }

        internal class E : Event
        {
            public MachineId Id;
            public int StageDelay;

            public E(MachineId id, int stageDelay)
            {
                this.Id = id;
                this.StageDelay = stageDelay;
            }
        }

        internal class S1Complete : Event { };

        internal class S2Complete : Event { };

        internal class S3Complete : Event { };

        internal class WorkerDone : Event { };

        internal class E1 : Event { };

        internal class E2 : Event { };

        internal class E3 : Event { };

        internal class Supervisor : Machine
        {
            public Supervisor() : base()
            {
                count = 0;
            }

            private TaskCompletionSource<bool> TCS;

            private int count;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(WorkerDone), nameof(Done))]
            private class Init : MachineState
            { }

            private void InitOnEntry()
            {
                TCS = (this.ReceivedEvent as Configure).TCS;
                var worker1 = CreateMachine(typeof(Worker));
                var worker2 = CreateMachine(typeof(Worker));
                var worker3 = CreateMachine(typeof(Worker));
                this.Send(worker1, new E(this.Id, 4));
                this.Send(worker2, new E(this.Id, 2));
                this.Send(worker3, new E(this.Id, 1));
            }

            private class State1 : MachineState { };

            private void Done()
            {
                count++;
                if (count == 3)
                {
                    TCS.SetResult(true);
                }
            }
        }

        internal class Worker : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E), nameof(Act))]
            [OnEventDoAction(typeof(E1), nameof(Stage1))]
            [OnEventDoAction(typeof(E2), nameof(Stage2))]
            [OnEventDoAction(typeof(E3), nameof(Stage3))]
            private class Init : MachineState
            { }

            private MachineId ParentId;
            private int Delay;

            private void Act()
            {
                this.ParentId = (this.ReceivedEvent as E).Id;
                this.Delay = (this.ReceivedEvent as E).StageDelay;
                this.Raise(new E1());
            }

            private async Task Stage1()
            {
                await Task.Delay(TimeSpan.FromSeconds(this.Delay));
                this.Raise(new E2());
            }

            private async Task Stage2()
            {
                await Task.Delay(TimeSpan.FromSeconds(this.Delay));
                this.Raise(new E3());
            }

            private async Task Stage3()
            {
                await Task.Delay(TimeSpan.FromSeconds(this.Delay));
                this.Send(this.ParentId, new WorkerDone());
            }
        }

        [Fact]
        public void Test1()
        {
            Configuration config = Configuration.Create().WithVerbosityEnabled(2).WithCriticalPathProfilingEnabled(true);
            PSharpRuntime runtime = PSharpRuntime.Create(config);
            config.OutputFilePath = @"C:\Users\t-ansant\Source\Repos\PSharp\bin\net46";
            runtime.SetDefaultCriticalPathProfiler();
            runtime.StartCriticalPathProfiling();
            var tcs = new TaskCompletionSource<bool>();
            runtime.CreateMachine(typeof(Supervisor), new Configure(tcs));
            tcs.Task.Wait();
            runtime.StopCriticalPathProfiling();
        }
    }
}