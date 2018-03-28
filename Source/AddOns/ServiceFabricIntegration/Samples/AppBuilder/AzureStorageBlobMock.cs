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

	class AzureStorageBlobMock : ReliableStateMachine
	{
		#region fields

		/// <summary>
		/// Store the set of transaction ids which have already been processed.
		/// </summary>
		IReliableDictionary<int, int> TxIdObserved;

		/// <summary>
		/// Handle to the blockchain.
		/// </summary>
		ReliableRegister<MachineId> Blockchain;

		/// <summary>
		/// Handle to the SQLDatabase machine.
		/// </summary>
		ReliableRegister<MachineId> SQLDatabase;

		IReliableDictionary<int, int> AccountsUnderProcessing;

		#endregion

		#region states
		[Start]
		[OnEntry(nameof(Initialize))]
		[OnEventDoAction(typeof(StorageBlobTransferEvent), nameof(StartAmountTransfer))]
		[OnEventDoAction(typeof(ValidateBalanceResponseEvent), nameof(PushTransferToBlockChain))]
		class Init : MachineState { }
		#endregion

		#region handlers
		private async Task Initialize()
		{
			StorageBlobInitEvent e = this.ReceivedEvent as StorageBlobInitEvent;
			await Blockchain.Set(CurrentTransaction, e.blockchain);
			await SQLDatabase.Set(CurrentTransaction, e.sqlDatabase);
		}

		private async Task StartAmountTransfer()
		{
			StorageBlobTransferEvent e = this.ReceivedEvent as StorageBlobTransferEvent;

			// Check if we have already received this transaction earlier
			bool IsTxReceived = await TxIdObserved.ContainsKeyAsync(CurrentTransaction, e.txid);
			// The exact-once semantics should ensure we haven't seen this txid earlier
			this.Assert(!IsTxReceived, "AzureStorageBlob: txId " + e.txid + " has been processed already");

			// Check if the "from" account is already under process. Akin to holding a "lock" on the "from" acc.
			// A hacky solution to prevent double-spending.
			/*if(await AccountsUnderProcessing.ContainsKeyAsync(CurrentTransaction, e.from))
			{
				await ReliableSend(await SQLDatabase.Get(CurrentTransaction), new UpdateTxStatusDBEvent(e.txid, "aborted"));
				return;
			}
			await AccountsUnderProcessing.AddAsync(CurrentTransaction, e.from, 0);
			*/
			// add the txid to the set of observed txids
			await TxIdObserved.AddAsync(CurrentTransaction, e.txid, 0);

			// validate the balances from the blockchain
			await ReliableSend(await Blockchain.Get(CurrentTransaction), new ValidateBalanceEvent(e, this.Id));
			

		}

		private async Task PushTransferToBlockChain()
		{
			ValidateBalanceResponseEvent ev = this.ReceivedEvent as ValidateBalanceResponseEvent;

			if(!ev.validation)
			{
				// Update the status of the tx in the database to aborted
				await ReliableSend(await SQLDatabase.Get(CurrentTransaction), new UpdateTxStatusDBEvent(ev.e.txid, "aborted"));
				return;
			}

			// Release the lock on the "from" account
			//Assert(await AccountsUnderProcessing.ContainsKeyAsync(CurrentTransaction, ev.e.from),
						//"AzureStorageBlob: account: " + ev.e.from + " should be under process");
			//await AccountsUnderProcessing.TryRemoveAsync(CurrentTransaction, ev.e.from);

			// Create the transaction object
			TxObject tx = new TxObject(ev.e.txid, ev.e.from, ev.e.to, ev.e.amount);
			await ReliableSend(await Blockchain.Get(CurrentTransaction), new BlockchainTxEvent(tx));

			// Update DB with status
			await ReliableSend(await SQLDatabase.Get(CurrentTransaction), new UpdateTxStatusDBEvent(tx.txid, "committed"));
		}


		#endregion

		#region methods
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="stateManager"></param>
		public AzureStorageBlobMock(IReliableStateManager stateManager) : base(stateManager) { }

		/// <summary>
		/// Initialize the reliable fields.
		/// </summary>
		/// <returns></returns>
		public override async Task OnActivate()
		{
			this.Logger.WriteLine("AzureStorageBlobMock starting.");
			TxIdObserved = await this.StateManager.GetOrAddAsync<IReliableDictionary<int, int>>
							(QualifyWithMachineName("TxIdObserved"));
			Blockchain = new ReliableRegister<MachineId>(QualifyWithMachineName("Blockchain"),
							this.StateManager, null);
			SQLDatabase = new ReliableRegister<MachineId>(QualifyWithMachineName("SQLDatabase"),
							this.StateManager, null);
			AccountsUnderProcessing = await this.StateManager.GetOrAddAsync<IReliableDictionary<int, int>>
							(QualifyWithMachineName("AccountsUnderProcessing"));

		}

		private string QualifyWithMachineName(string name)
		{
			return name + "_" + this.Id.Name;
		}

		#endregion
	}
}
