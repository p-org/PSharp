using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SpawnBenchmark.SpawnMachine;

namespace SpawnBenchmark
{
    class RootMachine : Machine
    {
        int N;
        long Start;
        public class Run : Event
        {
            public Run(int number)
            {
                Number = number;
            }

            public int Number { get; }
        }

        private static readonly Stopwatch Stopwatch = Stopwatch.StartNew();

        public class Initialize: Event
        {
            public Initialize(int n, long start)
            {
                N = n;
                Start = start;
            }
            public int N;
            public long Start;
        }

        [Microsoft.PSharp.Start]
        [OnEventDoAction(typeof(Run), nameof(StartRun))]        
        class Init : MachineState { }

        private void StartRun()
        {
            var e = this.ReceivedEvent as Run;
            N = e.Number;
            Console.WriteLine($"Start run {N}");
            Start = Stopwatch.ElapsedMilliseconds;
            this.CreateMachine(typeof(SpawnMachine), new SpawnMachine.Start(7, 0, this.Id));
            this.Goto<Waiting>();
            // Raise(new Initialize(n - 1, start));
            N--;
        }

        
        [OnEventDoAction(typeof(NumberEvent), nameof(HandleNumberEvent))]
        class Waiting : MachineState {  }

        
        private void HandleNumberEvent()
        {
            var e = this.ReceivedEvent as SpawnMachine.NumberEvent;
            var x = e.Number;
            var diff = (Stopwatch.ElapsedMilliseconds - Start);
            Console.WriteLine($"Run {N + 1} result: {x} in {diff} ms");
            if (N == 0)
            {
               // terminate all machines here? Figure out if there's a way to do
               // this in P#
            }
            else
            {
                this.Goto<Init>();
                this.Send(this.Id, new Run(N));
            }
        }
    }
}
