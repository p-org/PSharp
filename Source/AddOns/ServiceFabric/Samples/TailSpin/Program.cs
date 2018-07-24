using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp;
using Microsoft.PSharp.ServiceFabric;

namespace TailSpin
{
    /*
	 * General Idea: TailSpinCore is an application which:
	 * 1. Allows subscribers to register themselves.
	 * 2. Allows registered subscribers to create new surveys.
	 * 3. Conducts surveys and sends the responses back to the correct subscribers.
	 * 4. Allows registered subscribers to unregister themselves.
	 * 
	 * Each survey is just a sum of response.
	 * A response may either be a no-show, or a number between 0-10.
	 * Each survey lasts for 10sec.
	 * 
	 * The full scenario description is available at: https://msdn.microsoft.com/en-us/library/hh534482.aspx
	 */
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
            MachineId tsCore = runtime.CreateMachine(typeof(TailSpinCore));

            // Create 4 subcribers
            var subs1 = runtime.CreateMachine(typeof(Subscriber), new SubscriberInitEvent(tsCore));
            var subs2 = runtime.CreateMachine(typeof(Subscriber), new SubscriberInitEvent(tsCore));
            var subs3 = runtime.CreateMachine(typeof(Subscriber), new SubscriberInitEvent(tsCore));
            var subs4 = runtime.CreateMachine(typeof(Subscriber), new SubscriberInitEvent(tsCore));

            Console.ReadLine();
        }

        // The test contains a single TaipSpinCore application with 4 concurrent subscribers.
        [Test]
        public static void Execute(PSharpRuntime runtime)
        {
            var stateManager = new StateManagerMock(runtime);
            //stateManager.DisallowFailures();

            var origHost = RsmHost.CreateForTesting(stateManager, "SinglePartition", runtime);

            // Create the main TailSpinCore machine
            var tsCore = origHost.ReliableCreateMachine<TailSpinCore>(new RsmInitEvent()).Result;

            // Create 4 subcribers
            var subs1 = origHost.ReliableCreateMachine<Subscriber>(new SubscriberInitEvent(tsCore)).Result;
            var subs2 = origHost.ReliableCreateMachine<Subscriber>(new SubscriberInitEvent(tsCore)).Result;
            var subs3 = origHost.ReliableCreateMachine<Subscriber>(new SubscriberInitEvent(tsCore)).Result;
            var subs4 = origHost.ReliableCreateMachine<Subscriber>(new SubscriberInitEvent(tsCore)).Result;
        }

        private static void Runtime_OnFailure(Exception ex)
        {
            Console.WriteLine("Exception in the runtime: {0}", ex.Message);
            System.Diagnostics.Debugger.Launch();
        }
    }

}