using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PSharp;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;

namespace Microsoft.PSharp.ServiceFabric.Utilities
{
    /// <summary>
    /// A Reliable Register (Sequential)
    /// </summary>
    public class ReliableRegister<T> : RsmRegister
    {
        /// <summary>
        /// The state manager
        /// </summary>
        IReliableStateManager StateManager;

        /// <summary>
        /// Service cancellation token
        /// </summary>
        CancellationToken ServiceCancellationToken;

        /// <summary>
        /// Default time limit
        /// </summary>
        TimeSpan DefaultTimeLimit;

        /// <summary>
        /// Name of the counter
        /// </summary>
        string Name;

        /// <summary>
        ///  Initial counter value
        /// </summary>
        T InitialRegisterValue;

        /// <summary>
        /// The counter (at index 0)
        /// </summary>
        IReliableDictionary<int, T> Register;

        /// <summary>
        /// Currently executing transaction
        /// </summary>
        ITransaction CurrentTransaction;

        /// <summary>
        /// Creates a reliable counter meant only for sequential use
        /// </summary>
        /// <param name="name">Name of the counter</param>
        /// <param name="stateManager">StateManager</param>
        /// <param name="initialValue">The initial value of the counter, if its created for the first time</param>
        public ReliableRegister(string name, IReliableStateManager stateManager, T initialValue = default(T))
        {
            this.StateManager = stateManager;
            this.ServiceCancellationToken = CancellationToken.None;
            this.DefaultTimeLimit = TimeSpan.FromSeconds(4);
            this.Name = name;
            this.Register = null;
            this.InitialRegisterValue = initialValue;
        }

        /// <summary>
        /// Get the current counter value
        /// </summary>
        /// <returns>Register value</returns>
        public async Task<T> Get()
        {
            if(Register == null)
            {
                await InitializeRegister();
            }

            var cv = await Register.TryGetValueAsync(CurrentTransaction, 0, DefaultTimeLimit, ServiceCancellationToken);
            return cv.Value;
        }

        /// <summary>
        /// Set the counter value
        /// </summary>
        /// <param name="value">New value of the couter</param>
        /// <returns></returns>
        public async Task Set(T value)
        {
            if (Register == null)
            {
                await InitializeRegister();
            }
            await Register.AddOrUpdateAsync(CurrentTransaction, 0, value, (k, v) => value, DefaultTimeLimit, ServiceCancellationToken);
        }


        private async Task InitializeRegister()
        {
            Register = await StateManager.GetOrAddAsync<IReliableDictionary<int, T>>(Name);
            await Register.TryAddAsync(CurrentTransaction, 0, InitialRegisterValue, DefaultTimeLimit, ServiceCancellationToken);
        }

        internal override void SetTransaction(ITransaction tx)
        {
            this.CurrentTransaction = tx;
        }

        internal override void SetTransaction(ITransaction tx, TimeSpan timeSpan, CancellationToken cancellationToken)
        {
            this.CurrentTransaction = tx; 
            this.DefaultTimeLimit = timeSpan;
            this.ServiceCancellationToken = cancellationToken;
        }

    }

}
