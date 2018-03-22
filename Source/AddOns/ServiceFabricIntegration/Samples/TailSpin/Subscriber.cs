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
using System.Runtime.Serialization;
using Microsoft.ServiceFabric.Data.Collections;

namespace TailSpin
{
	class Subscriber : ReliableStateMachine
	{
		#region fields

		ReliableRegister<MachineId> TailSpinCoreMachine;

		ReliableRegister<int> SubscriberId;

		ReliableRegister<int> SurveyId;

		#endregion

		#region internal events

		[DataContract]
		class StartSurveyEvent : Event { }

		#endregion

		#region states

		[Start]
		[OnEntry(nameof(Initialize))]
		[OnEventDoAction(typeof(RegistrationSuccessEvent), nameof(HandleRegistrationSuccess))]
		[OnEventDoAction(typeof(StartSurveyEvent), nameof(InitiateSurvey))]
		[OnEventDoAction(typeof(SurveyCreationSuccessEvent), nameof(HandleSuccssfulSurveyCreation))]
		[OnEventDoAction(typeof(SurveyResultsEvent), nameof(PrintSurveyResults))]
		class Init : MachineState { }

		#endregion

		#region handlers
		async Task Initialize()
		{
			this.Logger.WriteLine("Subscriber " + this.Id + " starting");
			SubscriberInitEvent e = (this.ReceivedEvent as SubscriberInitEvent);
			await TailSpinCoreMachine.Set(CurrentTransaction, e.TailSpinCoreMachine);

			// Cache the handle to TailSpinCoreMachine
			MachineId tsCore = await TailSpinCoreMachine.Get(CurrentTransaction);

			// Register myself
			await this.ReliableSend(tsCore, new RegisterSubscriberEvent(this.Id));
		}

		async Task HandleRegistrationSuccess()
		{
			RegistrationSuccessEvent e = (this.ReceivedEvent as RegistrationSuccessEvent);
			this.Logger.WriteLine("Registration successful!");
			await SubscriberId.Set(CurrentTransaction, e.SubscriberId);
			this.Raise(new StartSurveyEvent());
		}

		async Task InitiateSurvey()
		{
			MachineId tsCore = await TailSpinCoreMachine.Get(CurrentTransaction);
			int subscriberId = await SubscriberId.Get(CurrentTransaction);

			await this.ReliableSend(tsCore, new CreateSurveyEvent(subscriberId));
		}

		void HandleSuccssfulSurveyCreation()
		{
			this.Logger.WriteLine("Survey created successfully!");
		}

		void PrintSurveyResults()
		{
			SurveyResultsEvent e = (this.ReceivedEvent as SurveyResultsEvent);
			this.Logger.WriteLine("Votes: " + e.response);
		}

		#endregion

		#region methods
		public Subscriber(IReliableStateManager stateManager) : base(stateManager) { }

		public override Task OnActivate()
		{
			TailSpinCoreMachine = new ReliableRegister<MachineId>(QualifyWithMachineName("TSCoreMachine"), this.StateManager, null);
			SubscriberId = new ReliableRegister<int>(QualifyWithMachineName("SubscriberId"), this.StateManager, 0);
			SurveyId = new ReliableRegister<int>(QualifyWithMachineName("SurveyId"), this.StateManager, 0);
			return Task.CompletedTask;
		}

		private string QualifyWithMachineName(string name)
		{
			return name + "_" + this.Id.Name;
		}
		#endregion
	}
}
