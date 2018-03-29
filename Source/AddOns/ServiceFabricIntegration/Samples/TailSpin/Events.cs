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

namespace TailSpin
{
	#region events
	
	/// <summary>
	/// Initialize the survey handler machine.
	/// </summary>
	[DataContract]
	class SurveyHandlerInitEvent : Event
	{
		/// <summary>
		/// Handle to the main TailSpinCore machine.
		/// </summary>
		[DataMember]
		public MachineId TailSpinCoreMachine;

		/// <summary>
		/// Specifies how long the survey is on
		/// </summary>
		[DataMember]
		public int SurveyDuration;

		/// <summary>
		/// Id of the subscriber who started the survey.
		/// </summary>
		[DataMember]
		public int SubscriberId;

		/// <summary>
		/// Id of the newly created survey.
		/// </summary>
		[DataMember]
		public int SurveyId;

		public SurveyHandlerInitEvent(MachineId TailSpinCoreMachine, int SurveyDuration, int SubscriberId, int SurveyId)
		{
			this.TailSpinCoreMachine = TailSpinCoreMachine;
			this.SurveyDuration = SurveyDuration;
			this.SubscriberId = SubscriberId;
			this.SurveyId = SurveyId;
		}
	}

	/// <summary>
	/// Event to initialize a new subscriber.
	/// </summary>
	[DataContract]
	class SubscriberInitEvent : Event
	{
		/// <summary>
		/// Handle to the TailSpinCore machine.
		/// </summary>
		[DataMember]
		public MachineId TailSpinCoreMachine;

		public SubscriberInitEvent(MachineId TailSpinCoreMachine)
		{
			this.TailSpinCoreMachine = TailSpinCoreMachine;
		}
	}

	/// <summary>
	/// A single survey response: models a set of votes.
	/// </summary>
	[DataContract]
	class SurveyResponse : Event
	{
		[DataMember]
		public int response;

		public SurveyResponse(int response)
		{
			this.response = response;
		}
	}

	/// <summary>
	/// Event using which a subscriber registers himself/herself with the TailSpinCore machine.
	/// In response, TailSpinCore will send a unique subscriber id.
	/// </summary>
	[DataContract]
	class RegisterSubscriberEvent : Event
	{
		/// <summary>
		/// MachineId of the subscriber who is trying to register.
		/// </summary>
		[DataMember]
		public MachineId Subscriber;

		public RegisterSubscriberEvent(MachineId Subscriber)
		{
			this.Subscriber = Subscriber;
		}
	}

	/// <summary>
	/// Response form TailSpinCore --> subscriber on successful registration.
	/// </summary>
	[DataContract]
	class RegistrationSuccessEvent : Event
	{
		[DataMember]
		public int SubscriberId;

		public RegistrationSuccessEvent(int SubscriberId)
		{
			this.SubscriberId = SubscriberId;
		}
	}

	/// <summary>
	/// Event using which a subscriber unregisters himself/herself.
	/// </summary>
	[DataContract]
	class UnregisterSubscriberEvent : Event
	{
		[DataMember]
		public int SubscriberId;

		public UnregisterSubscriberEvent(int SubscriberId)
		{
			this.SubscriberId = SubscriberId;
		}
	}

	/// <summary>
	/// Used by a subscriber to start a new survey.
	/// </summary>
	[DataContract]
	class CreateSurveyEvent : Event
	{
		[DataMember]
		public int SubscriberId;

		public CreateSurveyEvent(int SubscriberId)
		{
			this.SubscriberId = SubscriberId;
		}
	}

	/// <summary>
	/// Response from TailSpinCore --> subscriber on successful survey creation.
	/// </summary>
	[DataContract]
	class SurveyCreationSuccessEvent : Event
	{
		[DataMember]
		public int SurveyId;

		public SurveyCreationSuccessEvent(int SurveyId)
		{
			this.SurveyId = SurveyId;
		}
	}

	/// <summary>
	/// Response from Survey Handler --> TailSpinCore when a survey is complete.
	/// </summary>
	[DataContract]
	class SurveyResultsEvent : Event
	{
		[DataMember]
		public int SurveyId;

		[DataMember]
		public int response;

		public SurveyResultsEvent(int SurveyId, int response)
		{
			this.SurveyId = SurveyId;
			this.response = response;
		}
	}


	/// <summary>
	/// Response from TailSpinCore --> subscriber when a survey is completed.
	/// </summary>
	[DataContract]
	class CompletedSurveyEvent : Event
	{
		[DataMember]
		public int SubscriberId;

		[DataMember]
		public int SurveyId;

		[DataMember]
		public int FinalVotes;

		public CompletedSurveyEvent(int SubscriberId, int SurveyId, int FinalVotes)
		{
			this.SubscriberId = SubscriberId;
			this.SurveyId = SurveyId;
			this.FinalVotes = FinalVotes;
		}
	}
	#endregion
}
