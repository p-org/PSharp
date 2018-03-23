using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp;
using Microsoft.PSharp.ReliableServices;

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

			var config = Configuration.Create(); //.WithVerbosityEnabled(2);
			var runtime = PSharpRuntime.Create(config);
			runtime.OnFailure += Runtime_OnFailure;
			var stateManager = new StateManagerMock(runtime);
			runtime.AddMachineFactory(new ReliableStateMachineFactory(stateManager));
			
			// Create the main TailSpinCore machine
			var tsCore = runtime.CreateMachine(typeof(TailSpinCore));

			// Create 4 subcribers
			var subs1 = runtime.CreateMachine(typeof(Subscriber), new SubscriberInitEvent(tsCore));
			var subs2 = runtime.CreateMachine(typeof(Subscriber), new SubscriberInitEvent(tsCore));
			var subs3 = runtime.CreateMachine(typeof(Subscriber), new SubscriberInitEvent(tsCore));
			var subs4 = runtime.CreateMachine(typeof(Subscriber), new SubscriberInitEvent(tsCore));


			Console.ReadLine();
		}

		private static void Runtime_OnFailure(Exception ex)
		{
			Console.WriteLine("Runtime failure: {0}", ex.ToString());
			Console.Out.Flush();
			Environment.Exit(1);
		}

		// The test contains a single TaipSpinCore application with 4 concurrent subscribers.
		[Test]
		public static void Execute(PSharpRuntime runtime)
		{
			runtime.AddMachineFactory(new ReliableStateMachineFactory(new StateManagerMock(runtime), true));
			var tsCore = runtime.CreateMachine(typeof(TailSpinCore));
			var subs1 = runtime.CreateMachine(typeof(Subscriber), new SubscriberInitEvent(tsCore));
			var subs2 = runtime.CreateMachine(typeof(Subscriber), new SubscriberInitEvent(tsCore));
			var subs3 = runtime.CreateMachine(typeof(Subscriber), new SubscriberInitEvent(tsCore));
			var subs4 = runtime.CreateMachine(typeof(Subscriber), new SubscriberInitEvent(tsCore));

		}
	}

}
