using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessScheduler
{
    public class Test
    {
        static void Main(string[] args)
        {
            var runtime = PSharpRuntime.Create();
            Test.Execute(runtime);
            Console.ReadLine();
        }

        [Microsoft.PSharp.Test]
        public static void Execute(PSharpRuntime runtime)
        {
            runtime.RegisterMonitor(typeof(LivenessMonitor));
            runtime.CreateMachine(typeof(Environment));
            runtime.Wait();
            Console.WriteLine("Done!");
        }
    }
}
