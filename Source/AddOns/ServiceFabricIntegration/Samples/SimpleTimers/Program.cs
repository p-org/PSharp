using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp;
using Microsoft.PSharp.ReliableServices;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;

namespace SimpleTimers
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Diagnostics.Debugger.Launch();
            var config = Configuration.Create(); //.WithVerbosityEnabled(2);
            var runtime = PSharpRuntime.Create(config);
            runtime.OnFailure += Runtime_OnFailure;
            var stateManager = new StateManagerMock(runtime);
            runtime.AddMachineFactory(new ReliableStateMachineFactory(stateManager));
            var mid = runtime.CreateMachine(typeof(SimpleTimerMachine));

            Console.ReadLine();
        }

        private static void Runtime_OnFailure(Exception ex)
        {
            Console.WriteLine("Runtime failure: {0}", ex.ToString());
            Console.Out.Flush();
            Environment.Exit(1);
        }

        [Test]
        public static void Execute(PSharpRuntime runtime)
        {
            runtime.AddMachineFactory(new ReliableStateMachineFactory(new StateManagerMock(runtime), true));
            runtime.CreateMachine(typeof(SimpleTimerMachine));
        }
    }
}
