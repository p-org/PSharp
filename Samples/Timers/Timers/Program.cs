using System;
using Microsoft.PSharp;

namespace Timers
{
    class Program
    {

		static void Main(string[] args)
		{
			var configuration = Configuration.Create().WithVerbosityEnabled(1);
			configuration.EnableMonitorsInProduction = true;
			var runtime = PSharpRuntime.Create(configuration);
			Execute(runtime);
			Console.ReadLine();
		}

		[Microsoft.PSharp.Test]
		public static void Execute(PSharpRuntime runtime)
		{
			runtime.CreateMachine(typeof(Client));
		}
	}
}
