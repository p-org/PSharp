using Microsoft.PSharp;
using System;
using System.Threading.Tasks;
using Xunit;

namespace CriticalPathTests
{
    public class CascadeTest
    {
        internal class Configure : Event
        {
            public MachineId Id;
            public long Delay;

            public Configure(MachineId id, long delay)
            {
                this.Id = id;
                this.Delay = delay;
            }
        }

        internal class ConfigureFinisher : Event
        {
            public TaskCompletionSource<bool> TCS;
            public long Delay;

            public ConfigureFinisher(TaskCompletionSource<bool> tCS, long delay)
            {
                this.TCS = tCS;
                this.Delay = delay;
            }
        }

        internal class E : Event { };

        internal class InitiatorMachine : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            { }

            private void InitOnEntry()
            {
                var other = (this.ReceivedEvent as Configure).Id;
                var delay = (this.ReceivedEvent as Configure).Delay;
                Task.Delay(TimeSpan.FromSeconds(delay)).Wait();
                this.Send(other, new E());
            }
        }

        internal class ForwarderMachine : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            { }

            private void InitOnEntry()
            {
                var other = (this.ReceivedEvent as Configure).Id;
                var delay = (this.ReceivedEvent as Configure).Delay;
                Task.Delay(TimeSpan.FromSeconds(delay)).Wait();
                Receive(typeof(E));
                this.Send(other, new E());
            }
        }

        internal class FinisherMachine : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            { }

            private void InitOnEntry()
            {
                var tcs = (this.ReceivedEvent as ConfigureFinisher).TCS;
                var delay = (this.ReceivedEvent as ConfigureFinisher).Delay;
                Task.Delay(TimeSpan.FromSeconds(delay)).Wait();
                Receive(typeof(E));
                tcs.SetResult(true);
            }
        }

        [Fact]
        public void Test1()
        {
            Configuration config = Configuration.Create().WithVerbosityEnabled(2).WithCriticalPathProfilingEnabled(true);
            config.OutputFilePath = @"C:\Users\t-ansant\Source\Repos\PSharp\bin\net46";
            config.PAGFileName = "CascadeTest.Test1";
            PSharpRuntime runtime = PSharpRuntime.Create(config);

            var tcs = new TaskCompletionSource<bool>();
            runtime.SetDefaultCriticalPathProfiler();
            runtime.StartCriticalPathProfiling();
            var finisher = runtime.CreateMachine(typeof(FinisherMachine), new ConfigureFinisher(tcs, 3));
            var forwarder = runtime.CreateMachine(typeof(ForwarderMachine), new Configure(finisher, 5));
            var initiator = runtime.CreateMachine(typeof(InitiatorMachine), new Configure(forwarder, 10));
            tcs.Task.Wait();
            runtime.StopCriticalPathProfiling();
        }
    }
}