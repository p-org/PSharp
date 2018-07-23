using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp;
using Microsoft.PSharp.ServiceFabric;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;

namespace WordCount
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Diagnostics.Debugger.Launch();

            var stateManager = new StateManagerMock(null);
            stateManager.DisallowFailures();

            var config = Configuration.Create().WithVerbosityEnabled(2);
            var runtime = ServiceFabricRuntimeFactory.Create(stateManager, config);
            runtime.OnFailure += Runtime_OnFailure;

            runtime.CreateMachine(typeof(ClientMachine));

            Console.ReadLine();
        }

        /*
        [Test]
        public static void Execute(PSharpRuntime runtime)
        {
            runtime.RegisterMonitor(typeof(SafetyMonitor));

            var origHost = RsmHost.CreateForTesting(new StateManagerMock(runtime), "SinglePartition", runtime);
            origHost.ReliableCreateMachine<ClientMachine>(new RsmInitEvent());
        }
        */

        private static void Runtime_OnFailure(Exception ex)
        {
            Console.WriteLine("Exception in the runtime: {0}", ex.Message);
            System.Diagnostics.Debugger.Launch();
        }
    }

    static class Config
    {
        public static readonly int NumMachines = 2;
        public static readonly int StringLen = 2;
        public static readonly int NumWords = 3;
    }

}