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

	/// <summary>
	/// Sent by AppBuilder to register a new user.
	/// </summary>
	[DataContract]
	class RegisterNewUserEvent : Event { }

	/// <summary>
	/// Reply to AppBuilder with the public key of new user.
	/// </summary>
	[DataContract]
	class RegistrationResponseEvent : Event
	{
		[DataMember]
		public int response;
		
		public RegistrationResponseEvent(int response)
		{
			this.response = response;
		}
	}

	/// <summary>
	/// Return the private key associated with a user.
	/// -1: user does not exist, > 0: private key of valid user.
	/// </summary>
	[DataContract]
	class GetPrivateKeyEvent : Event
	{
		[DataMember]
		public int publicKey;

		public GetPrivateKeyEvent(int publicKey)
		{
			this.publicKey = publicKey;
		}
	}

	/// <summary>
	/// Return the private key associated with a user.
	/// -1: user does not exist, > 0: private key of valid user.
	/// </summary>
	[DataContract]
	class ReturnPrivateKeyEvent : Event
	{
		[DataMember]
		public int response;

		public ReturnPrivateKeyEvent(int response)
		{
			this.response = response;
		}
	}

	#endregion
	/// <summary>
	/// Mock of Azure Key Vault.
	/// Maps registered users to a private key.
	/// </summary>
	class AzureKeyVaultMock : ReliableStateMachine
	{
		#region fields

		/// <summary>
		/// Handle to the AppBuilder machine
		/// </summary>
		private ReliableRegister<MachineId> AppBuilderMachine;

		/// <summary>
		/// Public-key, private-key pair of a registered user
		/// </summary>
		private IReliableDictionary<int, int> RegisteredUsers;

		/// <summary>
		/// Monotonically increasing count of users registered thus far.
		/// </summary>
		private ReliableRegister<int> NumUsers;

		#endregion

		#region states
		[Start]
		[OnEntry(nameof(Initialize))]
		[OnEventDoAction(typeof(RegisterNewUserEvent), nameof(RegisterUser))]
		[OnEventDoAction(typeof(GetPrivateKeyEvent), nameof(ReturnPrivateKey))]
		class Init : MachineState { }
		#endregion

		#region handlers

		private async Task Initialize()
		{
			AzureKeyVaultInitEvent e = this.ReceivedEvent as AzureKeyVaultInitEvent;

			// Store the handle back to the AppBuilder machine
			await AppBuilderMachine.Set(CurrentTransaction, e.AppBuilderMachine);
		}

		/// <summary>
		/// Compute and reliably store public-private key of new user.
		/// </summary>
		/// <returns></returns>
		private async Task RegisterUser()
		{
			RegisterNewUserEvent e = (this.ReceivedEvent as RegisterNewUserEvent);

			int currentUsers = await NumUsers.Get(CurrentTransaction);

			// present value of NumUsers is the public key of the new users
			// private key is computed based on this public key
			int privateKey = ComputePrivateKey(currentUsers);

			// Verify that the public key is unique (uniqueness of private key follows from ComputePrivateKey)
			bool IsPublicKeyExists = await RegisteredUsers.ContainsKeyAsync(CurrentTransaction, currentUsers);
			this.Assert(!IsPublicKeyExists);

			// Store the public-private key pair 
			await RegisteredUsers.AddAsync(CurrentTransaction, currentUsers, privateKey);

			// return the public key to AppBuilder
			await this.ReliableSend(await AppBuilderMachine.Get(CurrentTransaction), new RegistrationResponseEvent(currentUsers));
		}

		/// <summary>
		/// Return private key of user back to AppBuilder.
		/// </summary>
		/// <returns></returns>
		private async Task ReturnPrivateKey()
		{
			GetPrivateKeyEvent e = this.ReceivedEvent as GetPrivateKeyEvent;

			// Check if the public key is valid
			bool IsValidPublicKey = await RegisteredUsers.ContainsKeyAsync(CurrentTransaction, e.publicKey);
			if(!IsValidPublicKey)
			{
				// return -1 for an invalid public key
				await this.ReliableSend(await AppBuilderMachine.Get(CurrentTransaction), new ReturnPrivateKeyEvent(-1));
			}
			else
			{
				int privateKey = (await RegisteredUsers.TryGetValueAsync(CurrentTransaction, e.publicKey)).Value;
				// return associated private key
				await this.ReliableSend(await AppBuilderMachine.Get(CurrentTransaction), new ReturnPrivateKeyEvent(privateKey));
			}
		}

		#endregion

		#region methods
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="stateManager"></param>
		public AzureKeyVaultMock(IReliableStateManager stateManager) : base(stateManager) { }

		/// <summary>
		/// Initialize the reliable fields.
		/// </summary>
		/// <returns></returns>
		public override async Task OnActivate()
		{
			this.Logger.WriteLine("AzureKeyVault starting.");

			RegisteredUsers = await this.StateManager.GetOrAddAsync<IReliableDictionary<int, int>>(QualifyWithMachineName("RegisteredUsers"));
			NumUsers = new ReliableRegister<int>(QualifyWithMachineName("NumUsers"), this.StateManager, 0);
			
		}

		private string QualifyWithMachineName(string name)
		{
			return name + "_" + this.Id.Name;
		}

		/// <summary>
		/// The current private key is simply publicKey + 1.
		/// </summary>
		/// <param name="publicKey">Public key of the user.</param>
		/// <returns>Private key corresponding to the public key.</returns>
		private int ComputePrivateKey(int publicKey)
		{
			return publicKey + 1;
		}
		#endregion
	}
}
