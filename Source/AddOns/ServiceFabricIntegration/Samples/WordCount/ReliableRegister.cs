using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp;
using Microsoft.PSharp.ReliableServices;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;

namespace WordCount
{
    /// <summary>
    /// A Reliable Register (Sequential)
    /// </summary>
    class ReliableRegister<T>
    {
        /// <summary>
        /// The state manager
        /// </summary>
        IReliableStateManager StateManager;

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
        /// Creates a reliable counter meant only for sequential use
        /// </summary>
        /// <param name="name">Name of the counter</param>
        /// <param name="stateManager">StateManager</param>
        /// <param name="initialValue">The initial value of the counter, if its created for the first time</param>
        public ReliableRegister(string name, IReliableStateManager stateManager, T initialValue = default(T))
        {
            this.StateManager = stateManager;
            this.Name = name;
            this.Register = null;
            this.InitialRegisterValue = initialValue;
        }

        /// <summary>
        /// Get the current counter value
        /// </summary>
        /// <param name="tx">Current transaction</param>
        /// <returns>Register value</returns>
        public async Task<T> Get(ITransaction tx)
        {
            if(Register == null)
            {
                await InitializeRegister(tx);
            }

            var cv = await Register.TryGetValueAsync(tx, 0);
            return cv.Value;
        }

        /// <summary>
        /// Set the counter value
        /// </summary>
        /// <param name="tx">Current transaction</param>
        /// <param name="value">New value of the couter</param>
        /// <returns></returns>
        public async Task Set(ITransaction tx, T value)
        {
            if (Register == null)
            {
                await InitializeRegister(tx);
            }
            await Register.AddOrUpdateAsync(tx, 0, value, (k, v) => value);
        }


        private async Task InitializeRegister(ITransaction tx)
        {
            Register = await StateManager.GetOrAddAsync<IReliableDictionary<int, T>>(Name);
            await Register.TryAddAsync(tx, 0, InitialRegisterValue);
        }

    }

}
