﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

using Microsoft.PSharp.TestingServices.Runtime;

namespace Microsoft.PSharp.SharedObjects
{
    /// <summary>
    /// A wrapper for a shared register modeled using a state-machine for testing.
    /// </summary>
    internal sealed class MockSharedRegister<T> : ISharedRegister<T>
        where T : struct
    {
        /// <summary>
        /// Machine modeling the shared register.
        /// </summary>
        private readonly MachineId RegisterMachine;

        /// <summary>
        /// The testing runtime hosting this shared register.
        /// </summary>
        private readonly SystematicTestingRuntime Runtime;

        /// <summary>
        /// Initializes a new instance of the <see cref="MockSharedRegister{T}"/> class.
        /// </summary>
        public MockSharedRegister(T value, SystematicTestingRuntime runtime)
        {
            this.Runtime = runtime;
            this.RegisterMachine = this.Runtime.CreateMachine(typeof(SharedRegisterMachine<T>));
            this.Runtime.SendEvent(this.RegisterMachine, SharedRegisterEvent.SetEvent(value));
        }

        /// <summary>
        /// Reads and updates the register.
        /// </summary>
        public T Update(Func<T, T> func)
        {
            var currentMachine = this.Runtime.GetExecutingMachine<Machine>();
            this.Runtime.SendEvent(this.RegisterMachine, SharedRegisterEvent.UpdateEvent(func, currentMachine.Id));
            var e = currentMachine.Receive(typeof(SharedRegisterResponseEvent<T>)).Result as SharedRegisterResponseEvent<T>;
            return e.Value;
        }

        /// <summary>
        /// Gets current value of the register.
        /// </summary>
        public T GetValue()
        {
            var currentMachine = this.Runtime.GetExecutingMachine<Machine>();
            this.Runtime.SendEvent(this.RegisterMachine, SharedRegisterEvent.GetEvent(currentMachine.Id));
            var e = currentMachine.Receive(typeof(SharedRegisterResponseEvent<T>)).Result as SharedRegisterResponseEvent<T>;
            return e.Value;
        }

        /// <summary>
        /// Sets current value of the register.
        /// </summary>
        public void SetValue(T value)
        {
            this.Runtime.SendEvent(this.RegisterMachine, SharedRegisterEvent.SetEvent(value));
        }
    }
}
