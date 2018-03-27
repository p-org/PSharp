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
	/// <summary>
	/// Mock of Azure Key Vault.
	/// Reliably stores the set of registered users.
	/// </summary>
	class AzureKeyVaultMock : ReliableStateMachine
	{
		#region fields

		/// <summary>
		/// Handle to the AppBuilder machine
		/// </summary>
		private ReliableRegister<MachineId> AppBuilderMachine;

		private IReliableDictionary<int, MachineId> RegisteredUsers;

		/// <summary>
		/// Public-key, private-key pair of a registered user
		/// </summary>
		//private IReliableDictionary<int, int> RegisteredUsers;

		/// <summary>
		/// Monotonically increasing count of users registered thus far.
		/// </summary>
		private ReliableRegister<int> NumUsers;

		#endregion

		#region states
		[Start]
		[OnEntry(nameof(Initialize))]
		[OnEventDoAction(typeof(UserRegisterEvent), nameof(RegisterUser))]
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
			UserRegisterEvent e = this.ReceivedEvent as UserRegisterEvent;

			int numUsers = await NumUsers.Get(CurrentTransaction);
			numUsers++;

			// Verify that the generated id is unique
			bool IDExists = await RegisteredUsers.ContainsKeyAsync(CurrentTransaction, numUsers);		
			this.Assert(!IDExists, "AzureKeyVault:RegisterUser(): Public key " + numUsers + " already exists");

			// Add new user
			await RegisteredUsers.AddAsync(CurrentTransaction, numUsers, e.user);

			// Update NumUsers
			await NumUsers.Set(CurrentTransaction, numUsers);

			// return the public key to AppBuilder
			await this.ReliableSend(await AppBuilderMachine.Get(CurrentTransaction), new RegistrationResponseEvent(numUsers, e.user));
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

			RegisteredUsers = await this.StateManager.GetOrAddAsync<IReliableDictionary<int, MachineId>>(QualifyWithMachineName("RegisteredUsers"));
			NumUsers = new ReliableRegister<int>(QualifyWithMachineName("NumUsers"), this.StateManager, 0);
			AppBuilderMachine = new ReliableRegister<MachineId>(QualifyWithMachineName("AppBuilderMachine"), this.StateManager, null);
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
