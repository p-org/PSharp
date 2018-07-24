using System;
using Microsoft.PSharp;
using Microsoft.PSharp.ServiceFabric;
using Microsoft.PSharp.ServiceFabric.TestingServices;

namespace BankAccount
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

        [TestInit]
        public static void TestStart()
        {
            //System.Diagnostics.Debugger.Launch();
        }

        [Test]
        public static void Execute(PSharpRuntime runtime)
        {
            runtime.RegisterMonitor(typeof(SafetyMonitor));
            var stateManager = new StateManagerMock(runtime);
           
            runtime.CreateMachine(typeof(ClientMachine));
        }

        private static void Runtime_OnFailure(Exception ex)
        {
            Console.WriteLine("Exception in the runtime: {0}", ex.Message);
            System.Diagnostics.Debugger.Launch();
        }
    }
}
