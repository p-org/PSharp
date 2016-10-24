using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp;

namespace MonitorInProduction
{
    class E : Event { }

    class Spec : Monitor
    {
        int counter = 0;

        [Start]
        [OnEventDoAction(typeof(E), nameof(foo))]
        class Init : MonitorState
        {  }

        void foo()
        {
            counter++;
            Console.WriteLine("Counter = {0}", counter);
            Assert(counter < 3);
        }

    }

    class Harness : Machine
    {
        [Start]
        [OnEntry(nameof(go))]
        class Init : MachineState { }

        void go()
        {
            Monitor<Spec>(new E());
            Monitor<Spec>(new E());
            Monitor<Spec>(new E());
            Monitor<Spec>(new E());
        }
    }

    class Program
    {
        public static void Main(string[] args)
        {
            var config = Microsoft.PSharp.Utilities.Configuration.Create();
            config.EnableMonitorsInProduction = true;

            var runtime = PSharpRuntime.Create(config);
            runtime.RegisterMonitor(typeof(Spec));
            runtime.CreateMachine(typeof(Harness));
            Console.ReadLine();
        }

        [Microsoft.PSharp.Test]
        public static void Execute(PSharpRuntime runtime)
        {
            runtime.RegisterMonitor(typeof(Spec));
            runtime.CreateMachine(typeof(Harness));
        }
    }
}
