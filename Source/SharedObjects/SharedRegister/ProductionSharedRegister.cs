// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.PSharp.SharedObjects
{
    /// <summary>
    /// Implements a shared register to be used in production.
    /// </summary>
    internal sealed class ProductionSharedRegister<T> : ISharedRegister<T> where T : struct
    {
        /// <summary>
        /// Current value of the register.
        /// </summary>
        T Value;

        /// <summary>
        /// Initializes the shared register.
        /// </summary>
        /// <param name="value">Initial value</param>
        public ProductionSharedRegister(T value)
        {
            Value = value;
        }

        /// <summary>
        /// Reads and updates the register.
        /// </summary>
        /// <param name="func">Update function</param>
        /// <returns>Resulting value of the register</returns>
        public T Update(Func<T, T> func)
        {
            T oldValue, newValue;
            bool done = false;

            do
            {
                oldValue = Value;
                newValue = func(oldValue);

                lock (this)
                {
                    if (oldValue.Equals(Value))
                    {
                        Value = newValue;
                        done = true;
                    }
                }
            } while (!done);

            return newValue;
        }

        /// <summary>
        /// Gets current value of the register.
        /// </summary>
        /// <returns>Current value</returns>
        public T GetValue()
        {
            T currentValue;
            lock (this)
            {
                currentValue = Value;
            }

            return currentValue;
        }

        /// <summary>
        /// Sets current value of the register.
        /// </summary>
        /// <param name="value">Value</param>
        public void SetValue(T value)
        {
            lock(this)
            {
                this.Value = value;
            }
        }
    }
}
