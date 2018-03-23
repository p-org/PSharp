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
	/*
	 * Each subscriber registers himself/herself, starts a survey, and when the survey completes, stores the result.
	 * 
	 */
	class Subscriber : ReliableStateMachine
	{
		#region fields

		ReliableRegister<MachineId> TailSpinCoreMachine;

		ReliableRegister<int> SubscriberId;
		IReliableDictionary<int, int> SurveyResponses;

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
		[OnEventDoAction(typeof(SurveyResultsEvent), nameof(RecordSurveyResults))]
		class Init : MachineState { }

		#endregion

		#region handlers
		async Task Initialize()
		{
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
			this.Logger.WriteLine("Subscriber " + this.Id + " registered with id: " + e.SubscriberId);
			await SubscriberId.Set(CurrentTransaction, e.SubscriberId);
			await this.ReliableSend(this.Id, new StartSurveyEvent());
		}

		async Task InitiateSurvey()
		{
			MachineId tsCore = await TailSpinCoreMachine.Get(CurrentTransaction);
			int subscriberId = await SubscriberId.Get(CurrentTransaction);

			await this.ReliableSend(tsCore, new CreateSurveyEvent(subscriberId));
		}

		async Task HandleSuccssfulSurveyCreation()
		{
			// this.Logger.WriteLine("Survey created successfully!");
			SurveyCreationSuccessEvent e = (this.ReceivedEvent as SurveyCreationSuccessEvent);

			// Verify that the obtained survey id is unique
			bool IsSurveyIdObserved = await SurveyResponses.ContainsKeyAsync(CurrentTransaction, e.SurveyId);
			this.Assert(!IsSurveyIdObserved, "Subscriber ID which is not unique");

			// Initialize the survey with some "invalid" response
			await SurveyResponses.AddAsync(CurrentTransaction, e.SurveyId, -1);
		}

		async Task RecordSurveyResults()
		{
			SurveyResultsEvent e = (this.ReceivedEvent as SurveyResultsEvent);
			int surveyId = e.SurveyId;
			int finalVotes = e.response;

			// Verify that the survey corresponds to a survey id received previously
			bool IsValidSurveyId = await SurveyResponses.ContainsKeyAsync(CurrentTransaction, e.SurveyId);
			this.Assert(IsValidSurveyId, "Survey ID is not valid");

			this.Logger.WriteLine("Subscriber ID: " + this.Id + " SurveyID: " + surveyId + " Votes: " + finalVotes);

			// Store the result
			await SurveyResponses.TryRemoveAsync(CurrentTransaction, surveyId);
			await SurveyResponses.AddAsync(CurrentTransaction, surveyId, finalVotes);

			// Unregister myself
			MachineId tsCore = await TailSpinCoreMachine.Get(CurrentTransaction);
			int subscriberId = await SubscriberId.Get(CurrentTransaction);
			await this.ReliableSend(tsCore, new UnregisterSubscriberEvent(subscriberId));
		}

		#endregion

		#region methods
		public Subscriber(IReliableStateManager stateManager) : base(stateManager) { }

		public override async Task OnActivate()
		{
			TailSpinCoreMachine = new ReliableRegister<MachineId>(QualifyWithMachineName("TSCoreMachine"), this.StateManager, null);
			SurveyResponses = await this.StateManager.GetOrAddAsync<IReliableDictionary<int, int>>(QualifyWithMachineName("SurveyResponses"));
			SubscriberId = new ReliableRegister<int>(QualifyWithMachineName("SubscriberId"), this.StateManager, 0);
			SurveyId = new ReliableRegister<int>(QualifyWithMachineName("SurveyId"), this.StateManager, 0);
		}

		private string QualifyWithMachineName(string name)
		{
			return name + "_" + this.Id.Name;
		}
		#endregion
	}
}
