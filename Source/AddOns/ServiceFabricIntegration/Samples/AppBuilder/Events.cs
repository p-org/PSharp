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
	class StorageBlobInitEvent : Event
	{
		[DataMember]
		public MachineId AppBuilderMachine;

		public StorageBlobInitEvent(MachineId AppBuilderMachine)
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

	#endregion

	#region transaction

	/// <summary>
	/// Raise a request to transfer ether from 'to' to 'from'
	/// </summary>
	[DataContract]
	class TransferEvent : Event
	{
		/// <summary>
		/// ID of account initiating transfer
		/// </summary>
		[DataMember]
		public int from;

		/// <summary>
		/// ID to which transfer is to be made
		/// </summary>
		[DataMember]
		public int to;

		/// <summary>
		/// Amount of ether to be transferred
		/// </summary>
		[DataMember]
		public int amount;

		public TransferEvent(int from, int to, int amount)
		{
			this.to = to;
			this.from = from;
			this.amount = amount;
		}
	}

	[DataContract]
	class TxIdEvent : Event
	{
		[DataMember]
		public int txid;

		public TxIdEvent(int txid)
		{
			this.txid = txid;
		}
	}
	#endregion
}
