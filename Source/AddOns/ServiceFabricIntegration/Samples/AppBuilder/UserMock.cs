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

		#endregion

	class UserMock : ReliableStateMachine
	{
		#region fields

		ReliableRegister<MachineId> AppBuilderMachine;

		ReliableRegister<int> Identifier;

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

			await this.ReliableSend(await AppBuilderMachine.Get(CurrentTransaction), new UserRegisterEvent(this.Id));
		}

		private async Task CompleteRegistration()
		{
			UserRegisterResponseEvent e = this.ReceivedEvent as UserRegisterResponseEvent;
			await Identifier.Set(CurrentTransaction, e.id);

			this.Logger.WriteLine("UserMock:CompleteRegistration() Public Key: " + await Identifier.Get(CurrentTransaction));
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
			return Task.CompletedTask;
		}

		private string QualifyWithMachineName(string name)
		{
			return name + "_" + this.Id.Name;
		}

		#endregion
	}
}
