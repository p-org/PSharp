using Microsoft.PSharp;
using System;
using System.Threading.Tasks;
using Xunit;

namespace CriticalPathTests
{
    public class ReceiveTest
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

            public E(MachineId id)
            {
                this.Id = id;
            }
        }

        internal class S1Complete : Event { };

        internal class S2Complete : Event { };

        internal class S3Complete : Event { };

        internal class MachineBDone : Event { };

        internal class E1 : Event { };

        internal class E2 : Event { };

        internal class E3 : Event { };

        internal class MachineA : Machine
        {
            private TaskCompletionSource<bool> TCS;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(S1))]
            private class Init : MachineState
            { }

            private void InitOnEntry()
            {
                TCS = (this.ReceivedEvent as Configure).TCS;
                var other = CreateMachine(typeof(MachineB));
                this.Send(other, new E(this.Id));
                Goto<State1>();
            }

            [OnEventGotoState(typeof(S1Complete), typeof(State2))]
            [OnEntry(nameof(S1))]
            private class State1 : MachineState { };

            private async Task S1()
            {
                await Task.Delay(TimeSpan.FromSeconds(3));
                this.Raise(new S1Complete());
            }

            [OnEventDoAction(typeof(S2Complete), nameof(S3))]
            [OnEntry(nameof(S2))]
            private class State2 : MachineState { };

            private async Task S2()
            {
                await Task.Delay(TimeSpan.FromSeconds(4));
                this.Raise(new S2Complete());
            }

            private async Task S3()
            {
                await Receive(typeof(MachineBDone));
                TCS.SetResult(true);
            }
        }

        internal class MachineB : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E), nameof(Act))]
            [OnEventDoAction(typeof(E1), nameof(Stage1))]
            [OnEventDoAction(typeof(E2), nameof(Stage2))]
            [OnEventDoAction(typeof(E3), nameof(Stage3))]
            private class Init : MachineState
            { }

            private MachineId ParentId;

            private void Act()
            {
                this.ParentId = (this.ReceivedEvent as E).Id;
                this.Raise(new E1());
            }

            private async Task Stage1()
            {
                await Task.Delay(TimeSpan.FromSeconds(3));
                this.Raise(new E2());
            }

            private async Task Stage2()
            {
                await Task.Delay(TimeSpan.FromSeconds(3));
                this.Raise(new E3());
            }

            private void Stage3()
            {
                this.Send(this.ParentId, new MachineBDone());
            }
        }

        [Fact]
        public void Test1()
        {
            Configuration config = Configuration.Create().WithVerbosityEnabled(2).WithCriticalPathProfilingEnabled(true);
            config.OutputFilePath = @"C:\PSharp\bin\net46";
            PSharpRuntime runtime = PSharpRuntime.Create(config);
            
            var tcs = new TaskCompletionSource<bool>();
            runtime.SetDefaultCriticalPathProfiler();
            runtime.StartCriticalPathProfiling();
            runtime.CreateMachine(typeof(MachineA), new Configure(tcs));
            tcs.Task.Wait();
            runtime.StopCriticalPathProfiling();
        }
    }
}