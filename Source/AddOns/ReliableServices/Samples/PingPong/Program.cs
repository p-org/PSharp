using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp;
using Microsoft.PSharp.ReliableServices;

namespace PingPong
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Diagnostics.Debugger.Launch();

            var stateManager = new StateManagerMock(null);
            stateManager.DisallowFailures();

            var origHost = RsmHost.Create(stateManager, "SinglePartition");
            origHost.ReliableCreateMachine<PingMachine>(new RsmInitEvent());

            Console.ReadLine();
        }

        [Test]
        public static void Execute(PSharpRuntime runtime)
        {
            System.Diagnostics.Debugger.Launch();
            var stateManager = new StateManagerMock(runtime);
            stateManager.DisallowFailures();

            var origHost = RsmHost.CreateForTesting(stateManager, "SinglePartition", runtime);
            origHost.ReliableCreateMachine<PingMachine>(new RsmInitEvent());
        }
    }
}
