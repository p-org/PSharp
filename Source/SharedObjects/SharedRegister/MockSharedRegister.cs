﻿// ------------------------------------------------------------------------------------------------
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
        private readonly MachineId RegisterMachine;

        /// <summary>
        /// The testing runtime hosting this shared register.
        /// </summary>
        private TestingRuntime Runtime;

        /// <summary>
        /// Initializes the shared register.
        /// </summary>
        /// <param name="value">Initial value</param>
        /// <param name="runtime">this.Runtime</param>
        public MockSharedRegister(T value, TestingRuntime runtime)
        {
            this.Runtime = runtime;
            this.RegisterMachine = this.Runtime.CreateMachine(typeof(SharedRegisterMachine<T>));
            this.Runtime.SendEvent(this.RegisterMachine, SharedRegisterEvent.SetEvent(value));
        }


        /// <summary>
        /// Reads and updates the register.
        /// </summary>
        /// <param name="func">Update function</param>
        /// <returns>Resulting value of the register</returns>
        public T Update(Func<T, T> func)
        {
            var currentMachine = this.Runtime.GetCurrentMachine();
            this.Runtime.SendEvent(this.RegisterMachine, SharedRegisterEvent.UpdateEvent(func, currentMachine.Id));
            var e = currentMachine.Receive(typeof(SharedRegisterResponseEvent<T>)).Result as SharedRegisterResponseEvent<T>;
            return e.Value;
        }

        /// <summary>
        /// Gets current value of the register.
        /// </summary>
        /// <returns>Current value</returns>
        public T GetValue()
        {
            var currentMachine = this.Runtime.GetCurrentMachine();
            this.Runtime.SendEvent(this.RegisterMachine, SharedRegisterEvent.GetEvent(currentMachine.Id));
            var e = currentMachine.Receive(typeof(SharedRegisterResponseEvent<T>)).Result as SharedRegisterResponseEvent<T>;
            return e.Value;
        }

        /// <summary>
        /// Sets current value of the register.
        /// </summary>
        /// <param name="value">Value</param>
        public void SetValue(T value)
        {
            this.Runtime.SendEvent(this.RegisterMachine, SharedRegisterEvent.SetEvent(value));
        }
    }
}
