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
	
	class AppBuilder : ReliableStateMachine
	{
		#region fields

		/// <summary>
		/// Models the Azure Key Vault
		/// </summary>
		IReliableDictionary<int, MachineId> RegisteredUsers;

		/// <summary>
		/// Set the number of users interacting with AppBuilder.
		/// </summary>
		ReliableRegister<int> NumUsers;

		/// <summary>
		/// Monotonically increasing count of the num users registered so far.
		/// </summary>
		ReliableRegister<int> CurrentNumUsers;

		/// <summary>
		/// Unique transaction id assigned to every transaction
		/// </summary>
		ReliableRegister<int> TxId;

		/// <summary>
		/// Handle to the storage blob, containing the dapps.
		/// </summary>
		ReliableRegister<MachineId> StorageBlobMachine;

		/// <summary>
		/// Handle to the mock of the SQL Database
		/// </summary>
		ReliableRegister<MachineId> SQLDatabaseMachine;

		/// <summary>
		/// Handle to the actual blockchain machine.
		/// </summary>
		ReliableRegister<MachineId> Blockchain;

		/// <summary>
		/// Mock of a bunch of users interacting with AppBuilder.
		/// </summary>
		ReliableRegister<MachineId> UserMock;

		

		#endregion

		#region states
		[Start]
		[OnEntry(nameof(Initialize))]
		[OnEventDoAction(typeof(RegisterUserEvent), nameof(RegisterUser))]
		[OnEventDoAction(typeof(TransferEvent), nameof(InitiateTransfer))]
		[OnEventDoAction(typeof(GetTxStatusDBEvent), nameof(ForwardTxStatusRequest))]
		[OnEventDoAction(typeof(TxDBStatus), nameof(ForwardTxStatusResponse))]
		class Init : MachineState { }
		#endregion

		#region handlers

		/// <summary>
		/// Create the component machines.
		/// </summary>
		/// <returns></returns>
		private async Task Initialize()
		{
			this.Logger.WriteLine("AppBuilder:Initialize()");
			MachineId userMock = await ReliableCreateMachine(typeof(UserMock), null,
								new UserMockInitEvent(this.Id, await NumUsers.Get(CurrentTransaction)));
			await UserMock.Set(CurrentTransaction, userMock);

			// Create the database where transaction statuses are kept
			MachineId sqlDatabase = await ReliableCreateMachine(typeof(SQLDatabaseMock), null,
						new SQLDatabaseInitEvent(this.Id));
			await SQLDatabaseMachine.Set(CurrentTransaction, sqlDatabase);

			// Create the blockchain
			MachineId blockchain = await ReliableCreateMachine(typeof(Blockchain), null);
			await Blockchain.Set(CurrentTransaction, blockchain);

			// Create Storage Blob 
			MachineId storageBlob = await ReliableCreateMachine(typeof(AzureStorageBlobMock), null,
						new StorageBlobInitEvent(blockchain, sqlDatabase));
			await StorageBlobMachine.Set(CurrentTransaction, storageBlob);

			// Create the blockchain printer
			MachineId blockchainPrinter = await ReliableCreateMachine(typeof(BlockchainPrinter), null,
						new BlockchainPrinterInitEvent(blockchain));
		}

		/// <summary>
		/// Register a new user. Here, we abstract away any authentication logic.
		/// In production, this would be substituted with a call to the AzureStorageVault service for authentication.
		/// </summary>
		/// <returns></returns>
		private async Task RegisterUser()
		{
			RegisterUserEvent e = this.ReceivedEvent as RegisterUserEvent;

			// Validate unique id
			bool IsIdExists = await RegisteredUsers.ContainsKeyAsync(CurrentTransaction, e.id);
			Assert(!IsIdExists, "Registered id: " + e.id + " is not unique");

			// Add to registered users
			await RegisteredUsers.AddAsync(CurrentTransaction, e.id, e.user);

			int currNumUsers = await CurrentNumUsers.Get(CurrentTransaction);
			await CurrentNumUsers.Set(CurrentTransaction, currNumUsers + 1);

			// #users registered must be less than the total number of users
			this.Assert(await CurrentNumUsers.Get(CurrentTransaction) <= await NumUsers.Get(CurrentTransaction),
					"AppBuilder: Num Registered users exceed NumUsers");
		}

		/// <summary>
		/// Initiate a transaction to transfer either from source acc --> dest acc
		/// Transfer of ether from A to B is the only operation supported in this sample.
		/// Additional ops can easily be added to AzureStorageBlobMock.
		/// </summary>
		/// <returns></returns>
		private async Task InitiateTransfer()
		{
			int currNumUsers = await CurrentNumUsers.Get(CurrentTransaction);
			int numUsers = await NumUsers.Get(CurrentTransaction);

			/*
			 * currNumUsers tracks the number of users registered so far.
			 * Since we know there are numUsers, we defer handling tx until everyone has been registered.
			*/
			if (currNumUsers != numUsers)
			{
				return;
			}

			TransferEvent e = this.ReceivedEvent as TransferEvent;

			// Verify if the source and destinatation accounts are registered
			bool SourceAccRegistered = await RegisteredUsers.ContainsKeyAsync(CurrentTransaction, e.from);
			bool DestAccRegistered = await RegisteredUsers.ContainsKeyAsync(CurrentTransaction, e.to);

			// Assign a new transaction id
			int txid = await TxId.Get(CurrentTransaction);
			txid++;

			// Set the transaction id
			await TxId.Set(CurrentTransaction, txid);

			// Abort the tx if one of the accounts isn't registered
			if ( !SourceAccRegistered && !DestAccRegistered)
			{
				// send back the txid to the user
				await ReliableSend(await UserMock.Get(CurrentTransaction), new TxIdEvent(txid));

				// record the status of the transaction in the SQLDatabase
				await ReliableSend(await SQLDatabaseMachine.Get(CurrentTransaction),
									new UpdateTxStatusDBEvent(txid, "aborted"));
				return;
			}

			// send back the txid to the user
			await ReliableSend(await UserMock.Get(CurrentTransaction), new TxIdEvent(txid));

			// start the transfer by invoking the appropriate action in StorageBlob
			// at present, we only support a simple transfer op from acc A to acc B
			await ReliableSend(await StorageBlobMachine.Get(CurrentTransaction),
								new StorageBlobTransferEvent(txid, e.from, e.to, e.amount));

			// record the status of the transaction in the SQLDatabase
			await ReliableSend(await SQLDatabaseMachine.Get(CurrentTransaction),
								new UpdateTxStatusDBEvent(txid, "processing"));

		}

		/// <summary>
		/// An enquiry from the user (UI) about the status of a tx is forwarded to the database machine
		/// </summary>
		/// <returns></returns>
		private async Task ForwardTxStatusRequest()
		{
			GetTxStatusDBEvent e = this.ReceivedEvent as GetTxStatusDBEvent;

			// Forward the TxStatus request to the SQL Database
			await ReliableSend(await SQLDatabaseMachine.Get(CurrentTransaction), e);
		}

		/// <summary>
		/// Once the tx status is received from the database machine, the response is fwd back to user (UI)
		/// </summary>
		/// <returns></returns>
		private async Task ForwardTxStatusResponse()
		{
			TxDBStatus e = this.ReceivedEvent as TxDBStatus;

			// Forward the TxStatus response from the database to the appropriate user
			await ReliableSend(e.requestFrom, e);
		}

		#endregion

		#region methods
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="stateManager"></param>
		public AppBuilder(IReliableStateManager stateManager) : base(stateManager) { }

		/// <summary>
		/// Initialize the reliable fields.
		/// </summary>
		/// <returns></returns>
		public override async Task OnActivate()
		{
			this.Logger.WriteLine("AppBuilder starting.");

			RegisteredUsers = await this.StateManager.GetOrAddAsync<IReliableDictionary<int, MachineId>>(QualifyWithMachineName("RegisteredUsers"));
			NumUsers = new ReliableRegister<int>(QualifyWithMachineName("NumUsers"), this.StateManager, 50);
			CurrentNumUsers = new ReliableRegister<int>(QualifyWithMachineName("CurrNumUsers"), this.StateManager, 0);
			TxId = new ReliableRegister<int>(QualifyWithMachineName("TxId"), this.StateManager, 0);
			StorageBlobMachine = new ReliableRegister<MachineId>(QualifyWithMachineName("StorageBlobMachine"), this.StateManager, null);
			SQLDatabaseMachine = new ReliableRegister<MachineId>(QualifyWithMachineName("SQLDatabaseMachine"), this.StateManager, null);
			Blockchain = new ReliableRegister<MachineId>(QualifyWithMachineName("Blockchain"), this.StateManager, null);
			UserMock = new ReliableRegister<MachineId>(QualifyWithMachineName("UserMock"), this.StateManager, null);
		}

		private string QualifyWithMachineName(string name)
		{
			return name + "_" + this.Id.Name;
		}

		#endregion
	}
}
