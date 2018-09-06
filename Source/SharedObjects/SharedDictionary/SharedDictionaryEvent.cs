// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

namespace Microsoft.PSharp.SharedObjects
{
    /// <summary>
    /// Event used to communicate with a shared counter machine.
    /// </summary>
    internal class SharedDictionaryEvent : Event
    {
        /// <summary>
        /// Supported shared dictionary operations.
        /// </summary>
        internal enum SharedDictionaryOperation { INIT, GET, SET, TRYADD, TRYGET, TRYUPDATE, TRYREMOVE, COUNT };

        /// <summary>
        /// The operation stored in this event.
        /// </summary>
        public SharedDictionaryOperation Operation { get; private set; }

        /// <summary>
        /// The shared dictionary key stored in this event.
        /// </summary>
        public object Key { get; private set; }

        /// <summary>
        /// The shared dictionary value stored in this event.
        /// </summary>
        public object Value { get; private set; }

        /// <summary>
        /// The shared dictionary comparison value stored in this event.
        /// </summary>
        public object ComparisonValue { get; private set; }

        /// <summary>
        /// The sender machine stored in this event.
        /// </summary>
        public MachineId Sender { get; private set; }

        /// <summary>
        /// The comparer stored in this event.
        /// </summary>
        public object Comparer { get; private set; }

        /// <summary>
        /// Creates a new event with the specified operation.
        /// </summary>
        /// <param name="op">SharedDictionaryOperation</param>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        /// <param name="comparisonValue">Comparison value</param>
        /// <param name="sender">Sender</param>
        /// <param name="comparer">Comparer</param>
        SharedDictionaryEvent(SharedDictionaryOperation op, object key, object value, object comparisonValue, MachineId sender, object comparer)
        {
            Operation = op;
            Key = key;
            Value = value;
            ComparisonValue = comparisonValue;
            Sender = sender;
            Comparer = comparer;
        }

        /// <summary>
        /// Creates a new event for the 'INIT' operation.
        /// </summary>
        /// <param name="comparer">Comparer</param>
        /// <returns>SharedDictionaryEvent</returns>
        public static SharedDictionaryEvent InitEvent(object comparer)
        {
            return new SharedDictionaryEvent(SharedDictionaryOperation.INIT, null, null, null, null, comparer);
        }

        /// <summary>
        /// Creates a new event for the 'TRYADD' operation.
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        /// <param name="sender">Sender</param>
        /// <returns>SharedDictionaryEvent</returns>
        public static SharedDictionaryEvent TryAddEvent(object key, object value, MachineId sender)
        {
            return new SharedDictionaryEvent(SharedDictionaryOperation.TRYADD, key, value, null, sender, null);
        }

        /// <summary>
        /// Creates a new event for the 'TRYUPDATE' operation.
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        /// <param name="comparisonValue">Comparison value</param>
        /// <param name="sender">Sender</param>
        /// <returns>SharedDictionaryEvent</returns>
        public static SharedDictionaryEvent TryUpdateEvent(object key, object value, object comparisonValue, MachineId sender)
        {
            return new SharedDictionaryEvent(SharedDictionaryOperation.TRYUPDATE, key, value, comparisonValue, sender, null);
        }

        /// <summary>
        /// Creates a new event for the 'GET' operation.
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="sender">Sender</param>
        /// <returns>SharedDictionaryEvent</returns>
        public static SharedDictionaryEvent GetEvent(object key, MachineId sender)
        {
            return new SharedDictionaryEvent(SharedDictionaryOperation.GET, key, null, null, sender, null);
        }

        /// <summary>
        /// Creates a new event for the 'TRYGET' operation.
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="sender">Sender</param>
        /// <returns>SharedDictionaryEvent</returns>
        public static SharedDictionaryEvent TryGetEvent(object key, MachineId sender)
        {
            return new SharedDictionaryEvent(SharedDictionaryOperation.TRYGET, key, null, null, sender, null);
        }

        /// <summary>
        /// Creates a new event for the 'SET' operation.
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        /// <returns>SharedDictionaryEvent</returns>
        public static SharedDictionaryEvent SetEvent(object key, object value)
        {
            return new SharedDictionaryEvent(SharedDictionaryOperation.SET, key, value, null, null, null);
        }

        /// <summary>
        /// Creates a new event for the 'COUNT' operation.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <returns>SharedDictionaryEvent</returns>
        public static SharedDictionaryEvent CountEvent(MachineId sender)
        {
            return new SharedDictionaryEvent(SharedDictionaryOperation.COUNT, null, null, null, sender, null);
        }

        /// <summary>
        /// Creates a new event for the 'TRYREMOVE' operation.
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="sender">Sender</param>
        /// <returns>SharedDictionaryEvent</returns>
        public static SharedDictionaryEvent TryRemoveEvent(object key, MachineId sender)
        {
            return new SharedDictionaryEvent(SharedDictionaryOperation.TRYREMOVE, key, null, null, sender, null);
        }

    }
}
