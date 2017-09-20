using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SingleTaskMachine
{
    class Program
    {
        public static readonly int N = 1;

        static void Main(string[] args)
        {
            var sw = new Stopwatch();
            var tcs = new TaskCompletionSource<bool>();

            var runtime = PSharpRuntime.Create();
            var m = runtime.CreateMachine(typeof(M), new Config(tcs));

            sw.Start();

            //if (args.Length == 0)
            //{
            //for (int i = 0; i < N; i++)
            //{
            //    Task.Run(async () =>
            //    {
            //        await Task.Yield();
            //        runtime.SendEvent(m, new E());
            //    });
            //}
            // }
            //else
            //{
            for (int i = 0; i < N; i++)
            {
                MyTaskRun(runtime, async (v) =>
                {
                    await Task.Yield();
                    v.MySend(m, new E());
                });
            }
            //}

            tcs.Task.Wait();

            Console.WriteLine("{0} Creations took: {1}", N, sw.Elapsed.TotalSeconds);
            Console.ReadKey();
        }

        static void MyTaskRun(PSharpRuntime runtime, Func<SingleTaskMachine, Task> function)
        {
            runtime.CreateMachine(typeof(SingleTaskMachine), new SingleTaskMachineEvent(function));
        }
    }

    class Config : Event
    {
        public TaskCompletionSource<bool> tcs;

        public Config(TaskCompletionSource<bool> tcs)
        {
            this.tcs = tcs;
        }
    }

    class E : Event { }

    [Fast]
    class M : Machine
    {
        TaskCompletionSource<bool> tcs;
        long counter;

        [Start]
        [OnEntry(nameof(Cons))]
        [OnEventDoAction(typeof(E), nameof(Inc))]
        class Init : MachineState { }

        void Cons()
        {
            this.tcs = (this.ReceivedEvent as Config).tcs;
            this.counter = 0;
        }

        void Inc()
        {
            counter++;            
            if(counter == Program.N)
            {
                tcs.SetResult(true);
            }
        }
    }
}
