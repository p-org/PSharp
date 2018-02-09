using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp;

namespace Timers
{
	class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine("Starting machines...");
			var configuration = Configuration.Create().WithVerbosityEnabled(1);
			var runtime = PSharpRuntime.Create(configuration);
			Execute(runtime);
			Console.ReadLine();
		}

		[Microsoft.PSharp.Test]
		public static void Execute(PSharpRuntime runtime)
		{
			// This is the root machine to the P# PingPong program. CreateMachine
			// executes asynchronously (i.e. non-blocking).
			runtime.CreateMachine(typeof(ClientPeriodicTimer));
		}
	}
}
