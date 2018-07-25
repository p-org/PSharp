using System;
using Microsoft.PSharp;
using Microsoft.PSharp.ServiceFabric;
using Microsoft.PSharp.ServiceFabric.TestingServices;

namespace PingPong
{
    class Program
    {
        static void Main(string[] args)
        {
            var stateManager = new StateManagerMock(null);
            stateManager.DisallowFailures();

            // Optional: increases verbosity level to see the P# runtime log.
            var configuration = Configuration.Create().WithVerbosityEnabled(2);

            // Creates a new Service Fabric P# runtime instance, and passes
            // the state manager and the configuration.
            var runtime = ServiceFabricRuntimeFactory.Create(stateManager, configuration);
            runtime.OnFailure += Runtime_OnFailure;

            // Executes the P# program.
            Program.Execute(runtime);

            // The P# runtime executes asynchronously, so we wait
            // to not terminate the process.
            Console.WriteLine("Press Enter to terminate...");
            Console.ReadLine();
        }
        
        [Test]
        public static void Execute(PSharpRuntime runtime)
        {
            runtime.RegisterMonitor(typeof(SafetyMonitor));
            runtime.RegisterMonitor(typeof(LivenessMonitor));
            runtime.CreateMachine(typeof(PingMachine));
        }

        private static void Runtime_OnFailure(Exception ex)
        {
            Console.WriteLine("Exception in the runtime: {0}", ex.Message);
            System.Diagnostics.Debugger.Launch();
        }
    }
}
