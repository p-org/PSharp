using Core.Utilities.Profiling;
using Microsoft.PSharp;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace CriticalPathTests
{
    public class CascadeTest
    {
        private readonly ITestOutputHelper output;

        public CascadeTest(ITestOutputHelper output)
        {
            this.output = output;
        }


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

            public ConfigureFinisher(TaskCompletionSource<bool> tcs, long delay)
            {
                this.TCS = tcs;
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
                var t = Task.Delay(TimeSpan.FromSeconds(delay));
                t.Wait();
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
                var t = Task.Delay(TimeSpan.FromSeconds(delay));
                t.Wait();
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
                var t = Task.Delay(TimeSpan.FromSeconds(delay));
                t.Wait();
                Receive(typeof(E));
                tcs.SetResult(true);
            }
        }

        [Fact]
        public void Test1()
        {
            Configuration config = Configuration.Create().WithVerbosityEnabled(2).WithCriticalPathProfilingEnabled(true);            
            config.PAGFileName = "CascadeTest.Test1";
            PSharpRuntime runtime = PSharpRuntime.Create(config);           
            config.OutputFilePath = @"C:\Users\t-ansant\Source\Repos\PSharp\bin\net46";
            var tcs = new TaskCompletionSource<bool>();
            var criticalPathProfiler = new CriticalPathProfiler(runtime);            
            var finisher = runtime.CreateMachine(typeof(FinisherMachine), new ConfigureFinisher(tcs, 3));
            var forwarder = runtime.CreateMachine(typeof(ForwarderMachine), new Configure(finisher, 5));
            var initiator = runtime.CreateMachine(typeof(InitiatorMachine), new Configure(forwarder, 10));
            tcs.Task.Wait();
            Task.Delay(TimeSpan.FromSeconds(5)).Wait();
            runtime.StopCriticalPathProfiling();
            criticalPathProfiler.Query("InitiatorMachine.Init:+ActionBegin:InitOnEntry", 4);
        }
    }
}