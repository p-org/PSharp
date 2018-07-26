using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp;
using Microsoft.PSharp.ServiceFabric;
using Microsoft.PSharp.ServiceFabric.TestingServices;

namespace DemoAppConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            var stateManager = new StateManagerMock(null);
            stateManager.DisallowFailures();

            // Optional: increases verbosity level to see the P# runtime log.
            var configuration = Configuration.Create();//.WithVerbosityEnabled(2);

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
            runtime.RegisterMonitor(typeof(PoolServicesContract.LivenessMonitor));
            PoolServicesContract.PoolDriverMachine.numVMsPerPool = 2;

            // Create a pool driver machine to process the client requests
            MachineId driver = runtime.CreateMachine(typeof(PoolServicesContract.PoolDriverMachine));

            // Create a client who fires off requests to the driver
            MachineId client = runtime.CreateMachine(typeof(ClientMachine), new eInitClient(driver));
        }

        private static void Runtime_OnFailure(Exception ex)
        {
            Console.WriteLine("Exception in the runtime: {0}", ex.Message);
            System.Diagnostics.Debugger.Launch();
        }
    }
}
