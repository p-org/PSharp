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

namespace AppBuilder
{
	class Blockchain : ReliableStateMachine
	{
		#region fields

		/// <summary>
		/// Set of uncommitted transactions
		/// </summary>
		IReliableQueue<TxObject> UncommittedTxPool;

		/// <summary>
		/// Caches the balances of each user.
		/// </summary>
		IReliableDictionary<int, int> Balances;

		/// <summary>
		/// Current block id
		/// </summary>
		ReliableRegister<int> BlockId;

		/// <summary>
		/// The ledger maps a block id to a set of committed transactions
		/// </summary>
		IReliableDictionary<int, TxBlock> Ledger;
		#endregion

		#region states
		[Start]
		[OnEntry(nameof(Initialize))]
		[OnEventDoAction(typeof(ValidateBalanceEvent), nameof(ValidateBalance))]
		class Init : MachineState { }
		#endregion

		#region handlers
		private async Task Initialize()
		{
			// Initialize the blockchain by giving 100 ether to the first 2 users!
			TxObject tx1 = new TxObject(-1, 0, 1, 100);
			TxObject tx2 = new TxObject(0, 0, 2, 100);

			// Create the genesis block
			TxBlock genesis = new TxBlock();
			genesis.numTx = 2;
			genesis.transactions.Add(tx1);
			genesis.transactions.Add(tx2);

			// Update the user balances
			await Balances.AddAsync(CurrentTransaction, 1, 100);
			await Balances.AddAsync(CurrentTransaction, 2, 100);
			this.Logger.WriteLine("First 2 users awarded 100 ether each!");

			// Add the genesis block to the ledger
			int blockId = await BlockId.Get(CurrentTransaction);
			await Ledger.AddAsync(CurrentTransaction, blockId, genesis);

			// Update the block id
			blockId++;
			await BlockId.Set(CurrentTransaction, blockId);
		}

		/// <summary>
		/// Check if the "from" account has enough balance to do a transaction
		/// </summary>
		/// <returns></returns>
		private async Task ValidateBalance()
		{
			ValidateBalanceEvent ev = this.ReceivedEvent as ValidateBalanceEvent;

			// Check if the "from" account has the requisite balance
			bool IsFromAccInBalances = await Balances.ContainsKeyAsync(CurrentTransaction, ev.e.from);

			if( !IsFromAccInBalances )
			{
				await ReliableSend(ev.requestFrom, new ValidateBalanceResponseEvent(ev.e, false));
				return;
			}

			int fromBalance = (await Balances.TryGetValueAsync(CurrentTransaction, ev.e.from)).Value;

			if(fromBalance < ev.e.amount)
			{
				await ReliableSend(ev.requestFrom, new ValidateBalanceResponseEvent(ev.e, false));
				return;
			}
			else
			{
				await ReliableSend(ev.requestFrom, new ValidateBalanceResponseEvent(ev.e, true));
				return;
			}
		}
		#endregion

		#region methods
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="stateManager"></param>
		public Blockchain(IReliableStateManager stateManager) : base(stateManager) { }

		/// <summary>
		/// Initialize the reliable fields.
		/// </summary>
		/// <returns></returns>
		public override async Task OnActivate()
		{
			this.Logger.WriteLine("Blockchain starting.");

			UncommittedTxPool = await this.StateManager.GetOrAddAsync<IReliableQueue<TxObject>>(QualifyWithMachineName("UncommittedTxPool"));

			Balances = await this.StateManager.GetOrAddAsync<IReliableDictionary<int, int>>(QualifyWithMachineName("Balances"));

			BlockId = new ReliableRegister<int>(QualifyWithMachineName("BlockId"), this.StateManager, 0);

			Ledger = await this.StateManager.GetOrAddAsync<IReliableDictionary<int, TxBlock>>(QualifyWithMachineName("Ledger"));
		}

		private string QualifyWithMachineName(string name)
		{
			return name + "_" + this.Id.Name;
		}
		#endregion
	}
}
