// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

using Microsoft.PSharp.TestingServices;

namespace Microsoft.PSharp.SharedObjects
{
    /// <summary>
    /// A wrapper for a shared register modeled using a state-machine for testing.
    /// </summary>
    internal sealed class MockSharedRegister<T> : ISharedRegister<T> where T: struct
    {
        /// <summary>
        /// Machine modeling the shared register.
        /// </summary>
        MachineId registerMachine;

        /// <summary>
        /// The bug-finding runtime hosting this shared register.
        /// </summary>
        BugFindingRuntime Runtime;

        /// <summary>
        /// Initializes the shared register.
        /// </summary>
        /// <param name="value">Initial value</param>
        /// <param name="Runtime">Runtime</param>
        public MockSharedRegister(T value, BugFindingRuntime Runtime)
        {
            this.Runtime = Runtime;
            registerMachine = Runtime.CreateMachine(typeof(SharedRegisterMachine<T>));
            Runtime.SendEvent(registerMachine, SharedRegisterEvent.SetEvent(value));
        }


        /// <summary>
        /// Reads and updates the register.
        /// </summary>
        /// <param name="func">Update function</param>
        /// <returns>Resulting value of the register</returns>
        public T Update(Func<T, T> func)
        {
            var currentMachine = Runtime.GetCurrentMachine();
            Runtime.SendEvent(registerMachine, SharedRegisterEvent.UpdateEvent(func, currentMachine.Id));
            var e = currentMachine.Receive(typeof(SharedRegisterResponseEvent<T>)).Result as SharedRegisterResponseEvent<T>;
            return e.Value;
        }

        /// <summary>
        /// Gets current value of the register.
        /// </summary>
        /// <returns>Current value</returns>
        public T GetValue()
        {
            var currentMachine = Runtime.GetCurrentMachine();
            Runtime.SendEvent(registerMachine, SharedRegisterEvent.GetEvent(currentMachine.Id));
            var e = currentMachine.Receive(typeof(SharedRegisterResponseEvent<T>)).Result as SharedRegisterResponseEvent<T>;
            return e.Value;
        }

        /// <summary>
        /// Sets current value of the register.
        /// </summary>
        /// <param name="value">Value</param>
        public void SetValue(T value)
        {
            Runtime.SendEvent(registerMachine, SharedRegisterEvent.SetEvent(value));
        }
    }
}
