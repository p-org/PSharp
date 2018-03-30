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
	/// Mock of a blockchain.
	/// </summary>
	class BlockchainMock : ReliableStateMachine
	{
		#region fields
		/// <summary>
		/// Set of uncommitted transactions
		/// </summary>
		IReliableConcurrentQueue<TxObject> UncommittedTxPool;

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

		/// <summary>
		/// Handle to the DLT machine
		/// </summary>
		ReliableRegister<MachineId> DLT;

		#endregion

		#region states
		[Start]
		[OnEventDoAction(typeof(BlockchainInitEvent), nameof(Initialize))]
		[OnEventDoAction(typeof(ValidateAndCommitEvent), nameof(ValidateAndCommit))]
		[OnEventDoAction(typeof(TimeoutEvent), nameof(CommitTxToLedger))]
		[OnEventDoAction(typeof(PrintLedgerEvent), nameof(PrintLedger))]
		class Init : MachineState { }
		#endregion

		#region handlers
		private async Task Initialize()
		{
			BlockchainInitEvent e = this.ReceivedEvent as BlockchainInitEvent;

			// Set handle to the dlt machine
			await DLT.Set(CurrentTransaction, e.dlt);

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

			// Start a timer. Every 5s, push all transactions from the uncommitted queue to the ledger.
			await StartTimer("CommitTxTimer", 5000);
		}

		/// <summary>
		/// Check if the "from" account has enough balance to do a transaction. 
		/// If validation passes, add the tx to the uncommitted pool. Pass tx status to the dlt.
		/// </summary>
		/// <returns></returns>
		private async Task ValidateAndCommit()
		{
			ValidateAndCommitEvent ev = this.ReceivedEvent as ValidateAndCommitEvent;

			// Check if the "from" account has the requisite balance
			bool IsFromAccInBalances = await Balances.ContainsKeyAsync(CurrentTransaction, ev.e.from);

			// If "from" isn't present in Balances, then it has never been cached => there are no prior tx which
			// has transferred ether to it.
			if( !IsFromAccInBalances )
			{
				await ReliableSend(await DLT.Get(CurrentTransaction), new UpdateTxStatusDBEvent(ev.e.txid, "aborted"));
				return;
			}

			int fromBalance = (await Balances.TryGetValueAsync(CurrentTransaction, ev.e.from)).Value;

			if(fromBalance < ev.e.amount)
			{
				await ReliableSend(await DLT.Get(CurrentTransaction), new UpdateTxStatusDBEvent(ev.e.txid, "aborted"));
				return;
			}
			else
			{
				/* Validation passed.
				 * Add tx to the uncommitted pool of tx.
				 * Note that we immediately set the status of the tx to committed, even though it has not yet been
				 * written to the ledger. Once a tx is in the uncommitted pool, it is guaranteed to be eventually 
				 * committed to the ledger (but it may take awhile). This is why we cache the balances so that 
				 * subsequent txs can go ahead.
				*/
				await ReliableSend(await DLT.Get(CurrentTransaction), new UpdateTxStatusDBEvent(ev.e.txid, "committed"));
				await AddNewTxToQueue(ev.e);
			}
		}

		/// <summary>
		/// Add the tx to the uncommitted pool of transactions.
		/// </summary>
		/// <param name="tx"></param>
		/// <returns></returns>
		private async Task AddNewTxToQueue(TxObject tx)
		{
			// Add the fresh transaction to the pool of uncommitted transactions
			await UncommittedTxPool.EnqueueAsync(CurrentTransaction, tx);

			// Update balances in cache
			int fromBalance = (await Balances.TryGetValueAsync(CurrentTransaction, tx.from)).Value;
			await Balances.TryRemoveAsync(CurrentTransaction, tx.from);
			await Balances.AddAsync(CurrentTransaction, tx.from, fromBalance - tx.amount);

			if (await Balances.ContainsKeyAsync(CurrentTransaction, tx.to))
			{
				int toBalance = (await Balances.TryGetValueAsync(CurrentTransaction, tx.to)).Value;
				await Balances.TryRemoveAsync(CurrentTransaction, tx.to);
				await Balances.AddAsync(CurrentTransaction, tx.to, toBalance + tx.amount);
			}
			else
			{
				await Balances.AddAsync(CurrentTransaction, tx.to, tx.amount);
			}
		}

		/// <summary>
		/// Commit the tx pending in the uncomitted pool to the ledger
		/// </summary>
		/// <returns></returns>
		private async Task CommitTxToLedger()
		{
			long numTxInQueue = UncommittedTxPool.Count;

			// No outstanding tx to commit
			if(numTxInQueue == 0)
			{
				return;
			}

			// Commit at most 5 pending tx to the ledger at a time
			long numToCommit = numTxInQueue < 5 ? numTxInQueue : 5;

			TxBlock txBlock = new TxBlock();

			for(long i=0; i<numToCommit; i++)
			{
				TxObject tx = (await UncommittedTxPool.TryDequeueAsync(CurrentTransaction)).Value;
				txBlock.numTx++;
				txBlock.transactions.Add(tx);
			}

			int blockId = await BlockId.Get(CurrentTransaction);
			await Ledger.AddAsync(CurrentTransaction, blockId, txBlock);

			// Update BlockId
			blockId++;
			await BlockId.Set(CurrentTransaction, blockId);
		}

		/// <summary>
		/// Pretty print the current status of the blockchain.
		/// </summary>
		/// <returns></returns>
		private async Task PrintLedger()
		{
			long numOutstandingTx = UncommittedTxPool.Count;
			long numBlocks = await Ledger.GetCountAsync(CurrentTransaction);

			this.Logger.WriteLine("\n****** Blockchain Status ******");
			this.Logger.WriteLine("#Outstanding transactions: " + numOutstandingTx);
			this.Logger.WriteLine("#Num blocks: " + numBlocks);

			for(int i=0; i<numBlocks; i++)
			{
				this.Logger.WriteLine("Block " + i + " --> ");
				TxBlock txBlock = (await Ledger.TryGetValueAsync(CurrentTransaction, i)).Value;
				foreach(var tx in txBlock.transactions)
				{
					this.Logger.WriteLine("Tx " + tx.txid + " Transfer " + tx.amount + " eth from " + tx.from + " to " + tx.to);
				}
				this.Logger.WriteLine("\n");
			}
			this.Logger.WriteLine("************");
		}
		#endregion

		#region methods
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="stateManager"></param>
		public BlockchainMock(IReliableStateManager stateManager) : base(stateManager) { }

		/// <summary>
		/// Initialize the reliable fields.
		/// </summary>
		/// <returns></returns>
		public override async Task OnActivate()
		{
			this.Logger.WriteLine("Blockchain starting.");

			UncommittedTxPool = await this.StateManager.GetOrAddAsync<IReliableConcurrentQueue<TxObject>>(QualifyWithMachineName("UncommittedTxPool"));
			Balances = await this.StateManager.GetOrAddAsync<IReliableDictionary<int, int>>(QualifyWithMachineName("Balances"));
			BlockId = new ReliableRegister<int>(QualifyWithMachineName("BlockId"), this.StateManager, 0);
			Ledger = await this.StateManager.GetOrAddAsync<IReliableDictionary<int, TxBlock>>(QualifyWithMachineName("Ledger"));
			DLT = new ReliableRegister<MachineId>(QualifyWithMachineName("DLT"), this.StateManager, null);
		}

		private string QualifyWithMachineName(string name)
		{
			return name + "_" + this.Id.Name;
		}
		#endregion
	}
}
