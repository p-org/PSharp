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

	[DataContract]
	class SurveyHandlerInitEvent : Event
	{
		[DataMember]
		public MachineId TailSpinCoreMachine;

		/// <summary>
		/// Specifies how long the survey is on
		/// </summary>
		[DataMember]
		public int SurveyDuration;

		[DataMember]
		public int SubscriberId;

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

	[DataContract]
	class SubscriberInitEvent : Event
	{
		[DataMember]
		public MachineId TailSpinCoreMachine;

		public SubscriberInitEvent(MachineId TailSpinCoreMachine)
		{
			this.TailSpinCoreMachine = TailSpinCoreMachine;
		}
	}

	[DataContract]
	/// <summary>
	/// A single survey response
	/// </summary>
	class SurveyResponse : Event
	{
		[DataMember]
		public int response;

		public SurveyResponse(int response)
		{
			this.response = response;
		}
	}

	[DataContract]
	class RegisterSubscriberEvent : Event
	{
		[DataMember]
		public MachineId Subscriber;

		public RegisterSubscriberEvent(MachineId Subscriber)
		{
			this.Subscriber = Subscriber;
		}
	}

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

	[DataContract]
	class AnalyzeSurveyEvent : Event
	{
		[DataMember]
		public int SurveyId;
		public int SubscriberId;

		public AnalyzeSurveyEvent(int SurveyId, int SubscriberId)
		{
			this.SurveyId = SurveyId;
			this.SubscriberId = SubscriberId;
		}
	}


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
