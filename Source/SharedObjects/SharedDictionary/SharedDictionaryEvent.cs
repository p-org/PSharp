﻿using
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
        internal enum SharedDictionaryOperation
        {
            INIT,
            GET,
            SET,
            TRYADD,
            TRYGET,
            TRYUPDATE,
            TRYREMOVE,
            COUNT
        }

        /// <summary>
        /// The operation stored in this event.
        /// </summary>
        internal SharedDictionaryOperation Operation { get; private set; }

        /// <summary>
        /// The shared dictionary key stored in this event.
        /// </summary>
        internal object Key { get; private set; }

        /// <summary>
        /// The shared dictionary value stored in this event.
        /// </summary>
        internal object Value { get; private set; }

        /// <summary>
        /// The shared dictionary comparison value stored in this event.
        /// </summary>
        internal object ComparisonValue { get; private set; }

        /// <summary>
        /// The sender machine stored in this event.
        /// </summary>
        internal MachineId Sender { get; private set; }

        /// <summary>
        /// The comparer stored in this event.
        /// </summary>
        internal object Comparer { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SharedDictionaryEvent"/> class.
        /// </summary>
        private SharedDictionaryEvent(SharedDictionaryOperation op, object key, object value, object comparisonValue, MachineId sender, object comparer)
        {
            this.Operation = op;
            this.Key = key;
            this.Value = value;
            this.ComparisonValue = comparisonValue;
            this.Sender = sender;
            this.Comparer = comparer;
        }

        /// <summary>
        /// Creates a new event for the 'INIT' operation.
        /// </summary>
        internal static SharedDictionaryEvent InitEvent(object comparer) =>
            new SharedDictionaryEvent(SharedDictionaryOperation.INIT, null, null, null, null, comparer);

        /// <summary>
        /// Creates a new event for the 'TRYADD' operation.
        /// </summary>
        internal static SharedDictionaryEvent TryAddEvent(object key, object value, MachineId sender) =>
            new SharedDictionaryEvent(SharedDictionaryOperation.TRYADD, key, value, null, sender, null);

        /// <summary>
        /// Creates a new event for the 'TRYUPDATE' operation.
        /// </summary>
        internal static SharedDictionaryEvent TryUpdateEvent(object key, object value, object comparisonValue, MachineId sender) =>
            new SharedDictionaryEvent(SharedDictionaryOperation.TRYUPDATE, key, value, comparisonValue, sender, null);

        /// <summary>
        /// Creates a new event for the 'GET' operation.
        /// </summary>
        internal static SharedDictionaryEvent GetEvent(object key, MachineId sender) =>
            new SharedDictionaryEvent(SharedDictionaryOperation.GET, key, null, null, sender, null);

        /// <summary>
        /// Creates a new event for the 'TRYGET' operation.
        /// </summary>
        internal static SharedDictionaryEvent TryGetEvent(object key, MachineId sender) =>
            new SharedDictionaryEvent(SharedDictionaryOperation.TRYGET, key, null, null, sender, null);

        /// <summary>
        /// Creates a new event for the 'SET' operation.
        /// </summary>
        internal static SharedDictionaryEvent SetEvent(object key, object value) =>
            new SharedDictionaryEvent(SharedDictionaryOperation.SET, key, value, null, null, null);

        /// <summary>
        /// Creates a new event for the 'COUNT' operation.
        /// </summary>
        internal static SharedDictionaryEvent CountEvent(MachineId sender) =>
            new SharedDictionaryEvent(SharedDictionaryOperation.COUNT, null, null, null, sender, null);

        /// <summary>
        /// Creates a new event for the 'TRYREMOVE' operation.
        /// </summary>
        internal static SharedDictionaryEvent TryRemoveEvent(object key, MachineId sender) =>
            new SharedDictionaryEvent(SharedDictionaryOperation.TRYREMOVE, key, null, null, sender, null);
    }
}
