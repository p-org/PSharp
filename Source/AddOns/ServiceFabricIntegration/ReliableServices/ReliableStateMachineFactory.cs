using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.ServiceFabric;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using System.Linq.Expressions;

namespace Microsoft.PSharp.ReliableServices
{
    public class ReliableStateMachineFactory : IMachineFactory
    {
        #region fields

        /// <summary>
        /// State Manager
        /// </summary>
        private IReliableStateManager StateManager;

        /// <summary>
        /// Create machines in test mode
        /// </summary>
        private bool InTestMode;

        /// <summary>
        /// Cache storing machine constructors.
        /// </summary>
        private Dictionary<Type, Func<IReliableStateManager, bool, Machine>> MachineConstructorCache;

        #endregion

        #region constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        public ReliableStateMachineFactory(IReliableStateManager StateManager, bool testMode = false)
        {
            this.StateManager = StateManager;
            MachineConstructorCache = new Dictionary<Type, Func<IReliableStateManager, bool, Machine>>();
            this.InTestMode = testMode;
        }

        #endregion

        #region methods

        /// <summary>
        /// Types for which this factory is responsible
        /// </summary>
        public Type BaseClassType()
        {
            return typeof(ReliableStateMachine);
        }

        /// <summary>
        /// Creates a new P# machine of the specified type.
        /// </summary>
        /// <param name="type">Type</param>
        /// <returns>Machine</returns>
        public Machine Create(Type type)
        {
            Machine newMachine;

            lock (MachineConstructorCache)
            {
                Func<IReliableStateManager, bool, Machine> constructor;
                if (!MachineConstructorCache.TryGetValue(type, out constructor))
                {
                    var param1 = Expression.Parameter(typeof(IReliableStateManager), "stateManager");
                    var param2 = Expression.Parameter(typeof(bool), "testMode");
                    constructor = Expression.Lambda<Func<IReliableStateManager, bool, Machine>>(
                        Expression.New(type.GetConstructor(new Type[] { typeof(IReliableStateManager), typeof(bool) }),
                        param1, param2), param1, param2).Compile();
                    MachineConstructorCache.Add(type, constructor);
                }

                newMachine = constructor(StateManager, InTestMode);
            }

            return newMachine;
        }

        #endregion
    }
}
