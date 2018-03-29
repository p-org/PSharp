using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp;
using Microsoft.PSharp.ReliableServices;
using Microsoft.PSharp.ReliableServices.Timers;
using Microsoft.PSharp.ReliableServices.Utilities;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;

namespace AppBuilder
{
	/// <summary>
	/// Models the distributed ledger technology.
	/// Handles the communication between the blockchain and sql database.
	/// </summary>
	class DLT : ReliableStateMachine
	{
		#region fields

		/// <summary>
		/// Handle to the blockchain machine
		/// </summary>
		ReliableRegister<MachineId> Blockchain;

		/// <summary>
		/// Handle to the sql database.
		/// </summary>
		ReliableRegister<MachineId> SqlDB;

		#endregion

		#region states
		[Start]
		[OnEventDoAction(typeof(DLTInitEvent), nameof(Initialize))]
		[OnEventDoAction(typeof(UpdateTxStatusDBEvent), nameof(FwdToDb))]
		class Init : MachineState { }
		#endregion

		#region handlers

		private async Task Initialize()
		{
			DLTInitEvent e = this.ReceivedEvent as DLTInitEvent;

			await Blockchain.Set(CurrentTransaction, e.blockchain);
			await SqlDB.Set(CurrentTransaction, e.sqldb);
		}

		/// <summary>
		/// Forward messages from blockchain to the database
		/// </summary>
		private async Task FwdToDb()
		{
			UpdateTxStatusDBEvent e = this.ReceivedEvent as UpdateTxStatusDBEvent;
			await ReliableSend(await SqlDB.Get(CurrentTransaction), e);
		}

		#endregion

		#region methods

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="stateManager"></param>
		public DLT(IReliableStateManager stateManager) : base(stateManager) { }

		/// <summary>
		/// Initialize the reliable fields.
		/// </summary>
		/// <returns></returns>
		public override Task OnActivate()
		{
			this.Logger.WriteLine("DLT starting.");

			Blockchain = new ReliableRegister<MachineId>(QualifyWithMachineName("Blockchain"), this.StateManager, null);
			SqlDB = new ReliableRegister<MachineId>(QualifyWithMachineName("SqlDB"), this.StateManager, null);

			return Task.CompletedTask;
		}

		private string QualifyWithMachineName(string name)
		{
			return name + "_" + this.Id.Name;
		}

		#endregion
	}
}
