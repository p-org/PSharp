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
            //System.Diagnostics.Debugger.Launch();

            var stateManager = new StateManagerMock(null);
            stateManager.DisallowFailures();

            var config = Configuration.Create().WithVerbosityEnabled(2);
            var origHost = RsmHost.Create(stateManager, "SinglePartition", config);
            origHost.ReliableCreateMachine<SimpleTimerMachine>(new RsmInitEvent());

            Console.ReadLine();
        }

        [Test]
        public static void Execute(PSharpRuntime runtime)
        {
            var stateManager = new StateManagerMock(runtime);
            stateManager.DisallowFailures();

            var origHost = RsmHost.CreateForTesting(stateManager, "SinglePartition", runtime);
            origHost.ReliableCreateMachine<SimpleTimerMachine>(new RsmInitEvent()).Wait();
        }
    }
}
