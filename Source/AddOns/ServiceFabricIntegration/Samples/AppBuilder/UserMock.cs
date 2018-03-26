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
	#region events

	[DataContract]
	class UserInitEvent : Event
	{
		[DataMember]
		public int id;

		[DataMember]
		public MachineId AppBuilderMachine;

		public UserInitEvent(int id, MachineId AppBuilderMachine)
		{
			this.id = id;
			this.AppBuilderMachine = AppBuilderMachine;
		}
	}

	#endregion

	class UserMock : ReliableStateMachine
	{
		#region fields

		ReliableRegister<MachineId> AppBuilderMachine;

		ReliableRegister<int> Identifier;

		ReliableRegister<int> PublicKey;

		#endregion

		#region states
		[Start]
		[OnEntry(nameof(Initialize))]
		[OnEventDoAction(typeof(UserRegisterResponseEvent), nameof(CompleteRegistration))]
		class Init : MachineState { }
		#endregion

		#region handler
		private async Task Initialize()
		{
			UserInitEvent e = this.ReceivedEvent as UserInitEvent;
			await AppBuilderMachine.Set(CurrentTransaction, e.AppBuilderMachine);
			await Identifier.Set(CurrentTransaction, e.id);

			await this.ReliableSend(await AppBuilderMachine.Get(CurrentTransaction), new UserRegisterEvent(this.Id));
		}

		private async Task CompleteRegistration()
		{
			UserRegisterResponseEvent e = this.ReceivedEvent as UserRegisterResponseEvent;
			await PublicKey.Set(CurrentTransaction, e.publicKey);

			this.Logger.WriteLine("UserMock:CompleteRegistration() Public Key: " + await PublicKey.Get(CurrentTransaction));
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
			PublicKey = new ReliableRegister<int>(QualifyWithMachineName("PublicKey"), this.StateManager, 0);
			Identifier = new ReliableRegister<int>(QualifyWithMachineName("Identifier"), this.StateManager, 0);
			return Task.CompletedTask;
		}

		private string QualifyWithMachineName(string name)
		{
			return name + "_" + this.Id.Name;
		}

		#endregion
	}
}
