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
	class SQLDatabaseMock : ReliableStateMachine
	{
		#region fields

		/// <summary>
		/// Stores the current status of each transaction.
		/// Status \in {Processing, Aborted, Committed}
		/// </summary>
		IReliableDictionary<int, string> TxStatus;

		#endregion

		#region states
		[Start]
		[OnEntry(nameof(Initialize))]
		[OnEventDoAction(typeof(UpdateTxStatusDBEvent), nameof(UpdateDB))]
		class Init : MachineState { }

		#endregion

		#region handlers
		private void Initialize()
		{
			this.Logger.WriteLine("SQLDatabaseMock starting");
		}

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
			// otherwise, remove the earlier status, and add the new one
			else
			{
				await TxStatus.TryRemoveAsync(CurrentTransaction, e.txid);
				await TxStatus.TryAddAsync(CurrentTransaction, e.txid, e.status);
			}
		}

		#endregion

		#region methods

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="stateManager"></param>
		public SQLDatabaseMock(IReliableStateManager stateManager) : base(stateManager) { }

		/// <summary>
		/// Initialize the reliable fields.
		/// </summary>
		/// <returns></returns>
		public async override Task OnActivate()
		{
			TxStatus = await this.StateManager.GetOrAddAsync<IReliableDictionary<int, string>>
									(QualifyWithMachineName("TxStatus"));
		}

		private string QualifyWithMachineName(string name)
		{
			return name + "_" + this.Id.Name;
		}

		#endregion
	}
}
