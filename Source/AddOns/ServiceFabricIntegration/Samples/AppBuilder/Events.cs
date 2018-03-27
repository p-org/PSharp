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
	#region machine initialization

	/// <summary>
	/// Initialize the key vault with a handle back to AppBuilder.
	/// </summary>
	[DataContract]
	class AzureKeyVaultInitEvent : Event
	{
		[DataMember]
		public MachineId AppBuilderMachine;

		public AzureKeyVaultInitEvent(MachineId AppBuilderMachine)
		{
			this.AppBuilderMachine = AppBuilderMachine;
		}
	}

	[DataContract]
	class UserInitEvent : Event
	{
		[DataMember]
		public MachineId AppBuilderMachine;

		public UserInitEvent(MachineId AppBuilderMachine)
		{
			this.AppBuilderMachine = AppBuilderMachine;
		}
	}

	#endregion

	#region user registration 

	/// <summary>
	/// Issued by user to register himself/herself with AppBuilder.
	/// </summary>
	[DataContract]
	class UserRegisterEvent : Event
	{
		[DataMember]
		public MachineId user;

		public UserRegisterEvent(MachineId user)
		{
			this.user = user;
		}
	}

	/// <summary>
	/// AppBuilder sends back the public key of new user on successful registration.
	/// </summary>
	[DataContract]
	class UserRegisterResponseEvent : Event
	{
		[DataMember]
		public int id;

		public UserRegisterResponseEvent(int id)
		{
			this.id = id;
		}
	}

	/// <summary>
	/// Reply to AppBuilder with the id of new user.
	/// </summary>
	[DataContract]
	class RegistrationResponseEvent : Event
	{
		[DataMember]
		public int response;

		[DataMember]
		public MachineId user;

		public RegistrationResponseEvent(int response, MachineId user)
		{
			this.response = response;
			this.user = user;
		}
	}

	#endregion

	#region transaction

	[DataContract]
	class TransferEvent : Event
	{
		[DataMember]
		public int to;

		[DataMember]
		public int from;

		[DataMember]
		public int amount;

		public TransferEvent(int to, int from, int amount)
		{
			this.to = to;
			this.from = from;
			this.amount = amount;
		}
	}

	#endregion
}
