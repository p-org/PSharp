using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp;
using Microsoft.PSharp.ReliableServices;

namespace TailSpin
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
			var tsCore = runtime.CreateMachine(typeof(TailSpinCore));
			var subs1 = runtime.CreateMachine(typeof(Subscriber), new SubscriberInitEvent(tsCore));

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
			// runtime.RegisterMonitor(typeof(SafetyMonitor));
			//runtime.CreateMachine(typeof(TailSpinCore));
		}
	}

}
