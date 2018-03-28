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

		ReliableRegister<int> Identifier;

		ReliableRegister<int> TxId;

		#endregion

		#region states
		[Start]
		[OnEntry(nameof(Initialize))]
		[OnEventDoAction(typeof(UserRegisterResponseEvent), nameof(CompleteRegistration))]
		[OnEventDoAction(typeof(TxIdEvent), nameof(NewTransaction))]
		[OnEventDoAction(typeof(TimeoutEvent), nameof(HandleTimeout))]
		[OnEventDoAction(typeof(TxDBStatus), nameof(HandleTxStatus))]
		class Init : MachineState { }
		#endregion

		#region handler
		private async Task Initialize()
		{
			UserInitEvent e = this.ReceivedEvent as UserInitEvent;
			await AppBuilderMachine.Set(CurrentTransaction, e.AppBuilderMachine);

			await this.ReliableSend(await AppBuilderMachine.Get(CurrentTransaction), new UserRegisterEvent(this.Id));
		}

		private async Task CompleteRegistration()
		{
			UserRegisterResponseEvent e = this.ReceivedEvent as UserRegisterResponseEvent;
			await Identifier.Set(CurrentTransaction, e.id);

			this.Logger.WriteLine("UserMock:CompleteRegistration() ID: " + await Identifier.Get(CurrentTransaction));

			// Initiate a transfer
			await this.ReliableSend(await AppBuilderMachine.Get(CurrentTransaction),
					new TransferEvent(1, 2, 10));
		}

		private async Task NewTransaction()
		{
			TxIdEvent e = this.ReceivedEvent as TxIdEvent;

			if(e.txid == -1)
			{
				this.Logger.WriteLine("UserMock:NewTransaction(): Failed to create new transaction");
				return;
			}
			
			this.Logger.WriteLine("UserMock:NewTransaction(): Transaction created successfully");
			await TxId.Set(CurrentTransaction, e.txid);

			await StartTimer(QualifyWithMachineName("PollTx"), 2000);
		}

		private async Task HandleTimeout()
		{
			// Enquire status of transaction
			await ReliableSend(await AppBuilderMachine.Get(CurrentTransaction), new GetTxStatusDBEvent(await TxId.Get(CurrentTransaction), this.Id));
		}

		private async Task HandleTxStatus()
		{
			TxDBStatus e = this.ReceivedEvent as TxDBStatus;

			if (e.txStatus == "committed" || e.txStatus == "aborted")
			{
				await StopTimer(QualifyWithMachineName("PollTx"));
				this.Logger.WriteLine("Tx " + e.txid + " " + e.txStatus);
			}
			else
			{
				this.Logger.WriteLine("Tx " + e.txid + " " + e.txStatus);
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
			AppBuilderMachine = new ReliableRegister<MachineId>(QualifyWithMachineName("AppBuilderMachine"), this.StateManager, null);
			Identifier = new ReliableRegister<int>(QualifyWithMachineName("Identifier"), this.StateManager, 0);
			TxId = new ReliableRegister<int>(QualifyWithMachineName("TxId"), this.StateManager, 0);
			return Task.CompletedTask;
		}

		private string QualifyWithMachineName(string name)
		{
			return name + "_" + this.Id.Name;
		}

		#endregion
	}
}
