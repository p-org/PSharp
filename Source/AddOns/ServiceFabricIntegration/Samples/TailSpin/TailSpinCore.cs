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


namespace TailSpin
{
	/// <summary>
	/// The machine which models the "Surveys" application.
	/// TailSpinCore handles subscriber registration, deletion, creation and management of surveys.
	/// </summary>
	class TailSpinCore : ReliableStateMachine
	{
		#region fields
		
		/// <summary>
		/// Reliably store the list of registered subscribers.
		/// </summary>
		IReliableDictionary<int, MachineId> RegisteredSubscribers;

		/// <summary>
		/// Reliably store a monotonically increasing count of number of subcribers who have registered so far.
		/// </summary>
		ReliableRegister<int> NumSubscribers;

		/// <summary>
		/// Reliably store the number of surveys created so far.
		/// </summary>
		ReliableRegister<int> NumSurveys;

		#endregion

		#region states

		[Start]
		[OnEntry(nameof(InitializeTailSpin))]
		[OnEventDoAction(typeof(RegisterSubscriberEvent), nameof(RegisterSubscriber))]
		[OnEventDoAction(typeof(UnregisterSubscriberEvent), nameof(UnregisterSubscriber))]
		[OnEventDoAction(typeof(CreateSurveyEvent), nameof(CreateSurvey))]
		[OnEventDoAction(typeof(CompletedSurveyEvent), nameof(UpdateCompletedSurvey))]
		class Init : MachineState { }

		#endregion

		#region handlers

		void InitializeTailSpin()
		{
			this.Logger.WriteLine("***Starting TailSpin Surveys***");
		}

		/// <summary>
		/// Register a subscriber by assigning a unique id.
		/// </summary>
		/// <returns></returns>
		async Task RegisterSubscriber()
		{
			RegisterSubscriberEvent e = (this.ReceivedEvent as RegisterSubscriberEvent);
			int currentNumSubscribers = await NumSubscribers.Get(CurrentTransaction);
			currentNumSubscribers++;

			// Ensure the id does not already exist among RegisteredSubscribers
			bool IsSubscriberAlreadyRegistered = await RegisteredSubscribers.ContainsKeyAsync(CurrentTransaction, currentNumSubscribers);
			this.Assert(!IsSubscriberAlreadyRegistered, "TailSpinCore: Subscriber with ID already exists");

			// Register subscriber
			await NumSubscribers.Set(CurrentTransaction, currentNumSubscribers);
			await RegisteredSubscribers.AddAsync(CurrentTransaction, currentNumSubscribers, e.Subscriber);

			// Send back the registration id
			await this.ReliableSend(e.Subscriber, new RegistrationSuccessEvent(currentNumSubscribers));

			// this.Logger.WriteLine("Subscriber registered successfully");
		}

		/// <summary>
		/// Unregister the subscriber by removing the reference from RegisteredSubscribers.
		/// </summary>
		/// <returns></returns>
		async Task UnregisterSubscriber()
		{
			UnregisterSubscriberEvent e = (this.ReceivedEvent as UnregisterSubscriberEvent);
			int subscriberId = e.SubscriberId;

			// Check if a valid subscriber is trying to unregister
			bool IsSubscriberRegistered = await RegisteredSubscribers.ContainsKeyAsync(CurrentTransaction, subscriberId);
			this.Assert(IsSubscriberRegistered, "TailSpinCore: Subscriber is not registered");

			// Remove the subscriber
			await RegisteredSubscribers.TryRemoveAsync(CurrentTransaction, subscriberId);

			this.Logger.WriteLine("Subscriber ID: " + subscriberId + " unregistered successfully");
		}

		/// <summary>
		/// Start a survey handler machine. Each survey runs for 10s.
		/// </summary>
		/// <returns></returns>
		async Task CreateSurvey()
		{
			CreateSurveyEvent e = (this.ReceivedEvent as CreateSurveyEvent);
			int subscriberId = e.SubscriberId;

			// Check if a valid subscriber is trying to create the survey
			bool IsSubscriberRegistered = await RegisteredSubscribers.ContainsKeyAsync(CurrentTransaction, subscriberId);
			this.Assert(IsSubscriberRegistered, "TailSpinCore: Subscriber " + subscriberId + " is not registered");

			// Update the survey ID
			int currentNumSurveys = await NumSurveys.Get(CurrentTransaction);
			currentNumSurveys++;

			await this.ReliableCreateMachine(typeof(SurveyHandlerMachine), null, new SurveyHandlerInitEvent(this.Id, 10000, subscriberId, currentNumSurveys));
			
			// Send ack back to subscriber
			MachineId mid = (await RegisteredSubscribers.TryGetValueAsync(CurrentTransaction, subscriberId)).Value;
			await this.ReliableSend(mid, new SurveyCreationSuccessEvent(currentNumSurveys));

			this.Logger.WriteLine("Survey " + currentNumSurveys + " started for subcscriber " + subscriberId);
		}

		/// <summary>
		/// On receiving a completed survey from a survey handler machine, sanitize and pass the response back to the subscriber.
		/// </summary>
		/// <returns></returns>
		async Task UpdateCompletedSurvey()
		{
			CompletedSurveyEvent e = (this.ReceivedEvent as CompletedSurveyEvent);
			int subscriberId = e.SubscriberId;
			int surveyId = e.SurveyId;
			int finalVotes = e.FinalVotes;

			// this.Logger.WriteLine("Survey " + surveyId + " completed with votes: " + finalVotes);

			// Check if the subscriber is still registered
			bool IsSubscriberRegistered = await RegisteredSubscribers.ContainsKeyAsync(CurrentTransaction, subscriberId);
			this.Assert(IsSubscriberRegistered, "TailSpinCore: Subscriber is not registered");

			// Return the survey result
			MachineId subscriber = (await RegisteredSubscribers.TryGetValueAsync(CurrentTransaction, subscriberId)).Value;
			await this.ReliableSend(subscriber, new SurveyResultsEvent(surveyId, finalVotes));
		}
		#endregion

		#region methods
		public TailSpinCore(IReliableStateManager stateManager) : base(stateManager) { }

		public override async Task OnActivate()
		{
			RegisteredSubscribers = await this.StateManager.GetOrAddAsync<IReliableDictionary<int, MachineId>>
				(QualifyWithMachineName("RegisteredSubscribers"));
			NumSubscribers = new ReliableRegister<int>(QualifyWithMachineName("NumSubscribers"), this.StateManager, 0);
			NumSurveys = new ReliableRegister<int>(QualifyWithMachineName("NumSurveys"), this.StateManager, 0);
		}

		private string QualifyWithMachineName(string name)
		{
			return name + "_" + this.Id.Name;
		}

		#endregion
	}
}
