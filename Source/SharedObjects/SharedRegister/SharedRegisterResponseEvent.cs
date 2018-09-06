// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

namespace Microsoft.PSharp.SharedObjects
{
    /// <summary>
    /// Event containing the value of a shared register.
    /// </summary>
    internal class SharedRegisterResponseEvent<T> : Event
    {
        /// <summary>
        /// Value.
        /// </summary>
        public T Value;

        /// <summary>
        /// Creates a new response event.
        /// </summary>
        /// <param name="value">Value</param>
        public SharedRegisterResponseEvent(T value)
        {
            Value = value;
        }
    }
}
