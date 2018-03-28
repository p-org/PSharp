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

	class UserMock : ReliableStateMachine
	{
		#region fields
		ReliableRegister<MachineId> AppBuilderMachine;
		ReliableRegister<int> NumUsers;
		ReliableRegister<HashSet<int>> TxIds;
		#endregion

		#region states
		[Start]
		[OnEntry(nameof(Initialize))]
		[OnEventDoAction(typeof(TimeoutEvent), nameof(HandleTimeout))]
		[OnEventDoAction(typeof(TxIdEvent), nameof(StoreTxId))]
		class Init : MachineState { }
		#endregion

		#region handler
		private async Task Initialize()
		{
			UserMockInitEvent e = this.ReceivedEvent as UserMockInitEvent;
			await AppBuilderMachine.Set(CurrentTransaction, e.AppBuilderMachine);
			await NumUsers.Set(CurrentTransaction, e.numUsers);

			// Register all the users
			for (int i = 1; i <= e.numUsers; i++)
			{
				await this.ReliableSend(await AppBuilderMachine.Get(CurrentTransaction), new RegisterUserEvent(i, null));
			}

			// Start generating transactions
			await StartTimer("TxTimer", 500);
		}


		private async Task StoreTxId()
		{
			TxIdEvent e = this.ReceivedEvent as TxIdEvent;

			if (e.txid == -1)
			{
				this.Logger.WriteLine("UserMock:NewTransaction(): Failed to create new transaction");
				return;
			}

			// this.Logger.WriteLine("UserMock:NewTransaction(): Transaction created successfully, id: " + e.txid);
			HashSet<int> txids = await TxIds.Get(CurrentTransaction);
			HashSet<int> new_txids = new HashSet<int>(txids);
			new_txids.Add(e.txid);
			await TxIds.Set(CurrentTransaction, new_txids);
		}

		private async Task HandleTimeout()
		{
			TimeoutEvent e = this.ReceivedEvent as TimeoutEvent;

			// Start a new transaction
			if (e.Name == "TxTimer")
			{
				int from = 0, to = 0;
				while (from == to)
				{
					from = RandomInteger(await NumUsers.Get(CurrentTransaction));
					to = RandomInteger(await NumUsers.Get(CurrentTransaction));
				}
				int amount = RandomInteger(100);

				await ReliableSend(await AppBuilderMachine.Get(CurrentTransaction), new TransferEvent(from, to, amount));
			}
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
		public override Task OnActivate()
		{
			NumUsers = new ReliableRegister<int>(QualifyWithMachineName("NumUsers"), this.StateManager, 0);
			AppBuilderMachine = new ReliableRegister<MachineId>(QualifyWithMachineName("AppBuilderMachine"), this.StateManager, null);
			TxIds = new ReliableRegister<HashSet<int>>(QualifyWithMachineName("TxIds"), this.StateManager, new HashSet<int>());
			return Task.CompletedTask;
		}

		private string QualifyWithMachineName(string name)
		{
			return name + "_" + this.Id.Name;
		}

		#endregion
	}
}
