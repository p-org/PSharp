using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp;
using Microsoft.PSharp.ReliableServices;
using Microsoft.PSharp.ReliableServices.Utilities;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using System.Runtime.Serialization;

namespace AppBuilder
{
	/// <summary>
	/// Mocks an SQL Database, which stores the status of all transactions. 
	/// The UI polls this to display the tx status, on a webpage (say).
	/// </summary>
	class SQLDatabase : ReliableStateMachine
	{
		#region fields

		/// <summary>
		/// Stores the current status of each transaction.
		/// Status \in {processing, aborted, committed}
		/// </summary>
		IReliableDictionary<int, string> TxStatus;

		/// <summary>
		/// Handle to the AppBuilder machine.
		/// </summary>
		ReliableRegister<MachineId> AppBuilderMachine;

		#endregion

		#region states
		[Start]
		[OnEntry(nameof(Initialize))]
		[OnEventDoAction(typeof(UpdateTxStatusDBEvent), nameof(UpdateDB))]
		[OnEventDoAction(typeof(GetTxStatusDBEvent), nameof(ReturnTxStatus))]
		class Init : MachineState { }

		#endregion

		#region handlers
		private async Task Initialize()
		{
			this.Logger.WriteLine("SQLDatabaseMock starting");
			SQLDatabaseInitEvent e = this.ReceivedEvent as SQLDatabaseInitEvent;
			await AppBuilderMachine.Set(CurrentTransaction, e.appBuilderMachine);
		}

		/// <summary>
		/// Update the database with the status of a tx.
		/// </summary>
		/// <returns></returns>
		private async Task UpdateDB()
		{
			UpdateTxStatusDBEvent e = this.ReceivedEvent as UpdateTxStatusDBEvent;

			// Check if the transaction already exists in the database
			bool txInDB = await TxStatus.ContainsKeyAsync(CurrentTransaction, e.txid);

			// for a new transaction, simply add the status
			if(!txInDB)
			{
				await TxStatus.AddAsync(CurrentTransaction, e.txid, e.status);
			}
			else
			{
				string currentStatus = (await TxStatus.TryGetValueAsync(CurrentTransaction, e.txid)).Value;

				// The transaction has already finished, don't update status further
				if(currentStatus == "aborted" || currentStatus == "committed")
				{
					return;
				}

				// otherwise, remove the earlier status, and add the new one
				await TxStatus.TryRemoveAsync(CurrentTransaction, e.txid);
				await TxStatus.TryAddAsync(CurrentTransaction, e.txid, e.status);
			}
		}

		/// <summary>
		/// Return the status of a tx.
		/// </summary>
		/// <returns></returns>
		private async Task ReturnTxStatus()
		{
			GetTxStatusDBEvent e = this.ReceivedEvent as GetTxStatusDBEvent;

			// Check if the transaction exists in the database
			bool txInDB = await TxStatus.ContainsKeyAsync(CurrentTransaction, e.txid);

			// Transaction status is always requested by users, which are forwarded by AppBuilder.
			// So responses are always returned to AppBuilder, who forwards it appropriately.
			if (!txInDB)
			{
				await ReliableSend(await AppBuilderMachine.Get(CurrentTransaction), 
							new TxDBStatus(e.txid, "status unavailable", e.requestFrom));
			}
			else
			{
				string currentStatus = (await TxStatus.TryGetValueAsync(CurrentTransaction, e.txid)).Value;
				await ReliableSend(await AppBuilderMachine.Get(CurrentTransaction),
							new TxDBStatus(e.txid, currentStatus, e.requestFrom));

			}
		}

		#endregion

		#region methods

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="stateManager"></param>
		public SQLDatabase(IReliableStateManager stateManager) : base(stateManager) { }

		/// <summary>
		/// Initialize the reliable fields.
		/// </summary>
		/// <returns></returns>
		public async override Task OnActivate()
		{
			TxStatus = await this.StateManager.GetOrAddAsync<IReliableDictionary<int, string>>
									(QualifyWithMachineName("TxStatus"));
			AppBuilderMachine = new ReliableRegister<MachineId>(QualifyWithMachineName("AppBuilderMachine"), this.StateManager, null);
		}

		private string QualifyWithMachineName(string name)
		{
			return name + "_" + this.Id.Name;
		}

		#endregion
	}
}
