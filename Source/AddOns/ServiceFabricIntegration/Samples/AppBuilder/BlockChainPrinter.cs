using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp;
using Microsoft.PSharp.ReliableServices;
using Microsoft.PSharp.ReliableServices.Utilities;
using Microsoft.PSharp.ReliableServices.Timers;
using Microsoft.ServiceFabric.Data;

namespace AppBuilder
{
	/// <summary>
	/// Prints out the entire blockchain at intervals of time
	/// </summary>
	class BlockchainPrinter : ReliableStateMachine
	{
		#region fields

		/// <summary>
		/// Handle to the blockchain
		/// </summary>
		ReliableRegister<MachineId> Blockchain;

		#endregion

		#region states
		[Start]
		[OnEntry(nameof(Initialize))]
		[OnEventDoAction(typeof(TimeoutEvent), nameof(HandleTimeout))]
		class Init : MachineState { }

		#endregion

		#region handlers
		private async Task Initialize()
		{
			BlockchainPrinterInitEvent e = this.ReceivedEvent as BlockchainPrinterInitEvent;
			await Blockchain.Set(CurrentTransaction, e.blockchain);
			await StartTimer("BlockchainPrinter", 5000);
		}

		private async Task HandleTimeout()
		{
			await ReliableSend(await Blockchain.Get(CurrentTransaction), new PrintLedgerEvent());
		}

		#endregion

		#region methods
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="stateManager"></param>
		public BlockchainPrinter(IReliableStateManager stateManager) : base(stateManager) { }

		/// <summary>
		/// Initialize the reliable fields.
		/// </summary>
		/// <returns></returns>
		public override Task OnActivate()
		{
			Blockchain = new ReliableRegister<MachineId>(QualifyWithMachineName("Blockchain"), this.StateManager, null);
			return Task.CompletedTask;
		}

		private string QualifyWithMachineName(string name)
		{
			return name + "_" + this.Id.Name;
		}
		#endregion
	}
}
