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
using Microsoft.ServiceFabric.Data.Collections;
using System.Runtime.Serialization;

namespace AppBuilder
{
	#region events

	#endregion

	/// <summary>
	/// Mocks a bunch of users interacting with the AppBuilder.
	/// </summary>
	class UserMock : ReliableStateMachine
	{
		#region fields
		/// <summary>
		/// Handle to the AppBuilder machine.
		/// </summary>
		ReliableRegister<MachineId> AppBuilderMachine;

		/// <summary>
		/// Handle to the sql database.
		/// </summary>
		ReliableRegister<MachineId> SqlDb;

		/// <summary>
		/// Preset the number of users in the system. Can be adjusted in AppBuilder.
		/// </summary>
		ReliableRegister<int> NumUsers;

		/// <summary>
		/// Store the set of transaction ids.
		/// </summary>
		IReliableDictionary<int, int> TxIds;
		#endregion

		#region states
		[Start]
		[OnEventDoAction(typeof(UserMockInitEvent), nameof(Initialize))]
		[OnEventDoAction(typeof(TimeoutEvent), nameof(HandleTimeout))]
		[OnEventDoAction(typeof(TxIdEvent), nameof(StoreTxId))]
		[OnEventDoAction(typeof(TxDBStatusResponseEvent), nameof(PrintTxStatus))]
		class Init : MachineState { }
		#endregion

		#region handler
		private async Task Initialize()
		{
			UserMockInitEvent e = this.ReceivedEvent as UserMockInitEvent;
			await AppBuilderMachine.Set(CurrentTransaction, e.appBuilderMachine);
			await SqlDb.Set(CurrentTransaction, e.sqlDb);
			await NumUsers.Set(CurrentTransaction, e.numUsers);

			// Register all the users
			for (int i = 1; i <= e.numUsers; i++)
			{
				await this.ReliableSend(await AppBuilderMachine.Get(CurrentTransaction), new RegisterUserEvent(i, null));
			}

			// Start generating transactions every 50ms
			await StartTimer("TxTimer", 50);

			// Poll the sql database every 5s
			await StartTimer("DbTimer", 5000);
		}

		/// <summary>
		/// Receive a fresh tx id for each initiated transaction.
		/// </summary>
		/// <returns></returns>
		private async Task StoreTxId()
		{
			TxIdEvent e = this.ReceivedEvent as TxIdEvent;

			if (e.txid == -1)
			{
				this.Logger.WriteLine("UserMock:NewTransaction(): Failed to create new transaction");
				return;
			}

			// Store the txid
			await TxIds.AddAsync(CurrentTransaction, e.txid, 0);
		}

		private async Task HandleTimeout()
		{
			TimeoutEvent e = this.ReceivedEvent as TimeoutEvent;

			// Start a new transaction
			if (e.Name == "TxTimer")
			{
				// Randomly generate the "from" and "to" accounts, and amount of ether to be transferred
				int from = 0, to = 0;
				while (from == to)
				{
					from = RandomInteger(await NumUsers.Get(CurrentTransaction));
					to = RandomInteger(await NumUsers.Get(CurrentTransaction));
				}
				int amount = RandomInteger(100);

				// Send request for the transfer to the AppBuilder
				await ReliableSend(await AppBuilderMachine.Get(CurrentTransaction), new TransferEvent(from, to, amount, this.Id));
			}
			// Periodically poll the status of some of the transactions
			if (e.Name == "DbTimer")
			{
				// Poll the database for the status of arbitrary transactions

				// get number of txs
				int numTx = (int) await TxIds.GetCountAsync(CurrentTransaction);

				int numTxToPoll = RandomInteger(numTx - 1) + 1;
				
				int[] txIdToPoll = new int[numTxToPoll];

				// Randomly generate the tx to poll
				for(int i=0; i<numTxToPoll; i++)
				{
					int tx = RandomInteger(numTx);

					if(await TxIds.ContainsKeyAsync(CurrentTransaction, tx))
					{
						txIdToPoll[i] = tx;
					}
					else
					{
						txIdToPoll[i] = 0;
					}
				}

				// Generate status requests
				for(int i=0; i<txIdToPoll.Length; i++)
				{
					await ReliableSend(await SqlDb.Get(CurrentTransaction), new GetTxStatusDBEvent(txIdToPoll[i], this.Id));
				}
			}
		}

		private void PrintTxStatus()
		{
			TxDBStatusResponseEvent e = this.ReceivedEvent as TxDBStatusResponseEvent;

			//this.Logger.WriteLine("Tx Status: " + e.txid + " : " + e.txStatus);
		}

		#endregion

		#region methods

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="stateManager"></param>
		public UserMock(IReliableStateManager stateManager) : base(stateManager) { }

		/// <summary>
		/// Initialize the reliable fields.
		/// </summary>
		/// <returns></returns>
		public override async Task OnActivate()
		{
			NumUsers = new ReliableRegister<int>(QualifyWithMachineName("NumUsers"), this.StateManager, 0);
			AppBuilderMachine = new ReliableRegister<MachineId>(QualifyWithMachineName("AppBuilderMachine"), this.StateManager, null);
			SqlDb = new ReliableRegister<MachineId>(QualifyWithMachineName("SqlDb"), this.StateManager, null);
			TxIds = await this.StateManager.GetOrAddAsync<IReliableDictionary<int, int>>(QualifyWithMachineName("TxIds"));
		}

		private string QualifyWithMachineName(string name)
		{
			return name + "_" + this.Id.Name;
		}

		#endregion
	}
}
