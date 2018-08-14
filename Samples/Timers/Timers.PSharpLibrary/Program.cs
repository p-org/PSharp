using System;
using Microsoft.PSharp;

namespace TimerSample
{
    class Program
    {
		static void Main(string[] args)
		{
			var configuration = Configuration.Create().WithVerbosityEnabled(1);
			var runtime = RuntimeService.Create(configuration);
			Execute(runtime);
			Console.ReadLine();
		}

		[Microsoft.PSharp.Test]
		public static void Execute(IPSharpRuntime runtime)
		{
			/*
			 * By default, StartTimer would create a machine of type TimerProduction, when running in production mode,
			 * and a machine of type TimerModel, when running in test. You can change the type of the timer using:
			 *
			 * runtime.SetTimerMachineType(<YourTimerMachineType>);
			 *
			 * Currently, YourTimerMachineType must be a subclass of Machine.
			 *
			*/
			runtime.CreateMachine(typeof(Client));
		}
	}
}
