using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp;
using Microsoft.PSharp.ReliableServices;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;

namespace AppBuilder
{
	/// <summary>
	/// Model of AppBuilder. There are 5 components that are modeled:
	/// 1. Azure Key Vault (for authentication): merged with AppBuilder here, for simplicity
	/// 2. Azure Storage Blob: where dapps are hosted (here we only support a simple transfer op)
	/// 3. SQL Database: where tx statuses are kept, to be polled by the UI
	/// 4. Blockchain: mocked here by reliable collections
	/// 5. AppBuilder: orchestrates all the components.
	/// </summary>
	class Program
	{
		static void Main(string[] args)
		{
			System.Diagnostics.Debugger.Launch();

			var config = Configuration.Create(); // .WithVerbosityEnabled(2);
			var runtime = PSharpRuntime.Create(config);
			runtime.OnFailure += Runtime_OnFailure;
			var stateManager = new StateManagerMock(runtime);
			runtime.AddMachineFactory(new ReliableStateMachineFactory(stateManager));

			// Start off the AppBuilder
			// Users are mocked in UserMock. The number of users is controlled in AppBuilder and set to 100 by default.
			MachineId sqldb = runtime.CreateMachine(typeof(SQLDatabase));
			MachineId users = runtime.CreateMachine(typeof(UserMock));
			MachineId blockchain = runtime.CreateMachine(typeof(BlockchainMock));
			MachineId appBuilder = runtime.CreateMachine(typeof(AppBuilder));
			

			// Initialize and start off the machines
			runtime.SendEvent(appBuilder, new AppBuilderInitEvent(blockchain, sqldb));
			
			// Start off with a bunch of users
			runtime.SendEvent(users, new UserMockInitEvent(appBuilder, sqldb, 100));

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

			// NumUsers set in AppBuilder and set to 100 by default
			MachineId AppBuilder = runtime.CreateMachine(typeof(AppBuilder));
		}
	}

}
