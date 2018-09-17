// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Microsoft.PSharp.SharedObjects
{
    /// <summary>
    /// A shared dictionary modeled using a state-machine for testing.
    /// </summary>
    internal sealed class SharedDictionaryMachine<TKey, TValue> : Machine 
    {
        /// <summary>
        /// The internal shared dictionary.
        /// </summary>
        Dictionary<TKey, TValue> Dictionary;

        /// <summary>
        /// The start state of this machine.
        /// </summary>
        [Start]
        [OnEntry(nameof(Initialize))]
        [OnEventDoAction(typeof(SharedDictionaryEvent), nameof(ProcessEvent))]
        class Init : MachineState { }

        /// <summary>
        /// Initializes the machine.
        /// </summary>
        void Initialize()
        {
            var e = (ReceivedEvent as SharedDictionaryEvent);

            if (e == null)
            {
                Dictionary = new Dictionary<TKey, TValue>();
                return;
            }

            if (e.Operation == SharedDictionaryEvent.SharedDictionaryOperation.INIT && e.Comparer != null)
            {
                Dictionary = new Dictionary<TKey, TValue>(e.Comparer as IEqualityComparer<TKey>);
            }
            else
            {
                throw new ArgumentException("Incorrect arguments provided to SharedDictionary.");
            }
        }

        /// <summary>
        /// Processes the next dequeued event.
        /// </summary>
        void ProcessEvent()
        {
            var e = ReceivedEvent as SharedDictionaryEvent;
            switch (e.Operation)
            {
                case SharedDictionaryEvent.SharedDictionaryOperation.TRYADD:
                    if (Dictionary.ContainsKey((TKey)e.Key))
                    {
                        Send(e.Sender, new SharedDictionaryResponseEvent<bool>(false));
                    }
                    else
                    {
                        Dictionary[(TKey)e.Key] = (TValue)e.Value;
                        Send(e.Sender, new SharedDictionaryResponseEvent<bool>(true));
                    }

                    break;

                case SharedDictionaryEvent.SharedDictionaryOperation.TRYUPDATE:
                    if (!Dictionary.ContainsKey((TKey)e.Key))
                    {
                        Send(e.Sender, new SharedDictionaryResponseEvent<bool>(false));
                    }
                    else
                    {
                        var currentValue = Dictionary[(TKey)e.Key];
                        if (currentValue.Equals((TValue)e.ComparisonValue))
                        {
                            Dictionary[(TKey)e.Key] = (TValue)e.Value;
                            Send(e.Sender, new SharedDictionaryResponseEvent<bool>(true));
                        }
                        else
                        {
                            Send(e.Sender, new SharedDictionaryResponseEvent<bool>(false));
                        }
                    }

                    break;

                case SharedDictionaryEvent.SharedDictionaryOperation.TRYGET:
                    if (!Dictionary.ContainsKey((TKey)e.Key))
                    {
                        Send(e.Sender, new SharedDictionaryResponseEvent<Tuple<bool, TValue>>(Tuple.Create(false, default(TValue))));
                    }
                    else
                    {
                        Send(e.Sender, new SharedDictionaryResponseEvent<Tuple<bool, TValue>>(Tuple.Create(true, Dictionary[(TKey)e.Key])));
                    }
                    break;

                case SharedDictionaryEvent.SharedDictionaryOperation.GET:
                    Send(e.Sender, new SharedDictionaryResponseEvent<TValue>(Dictionary[(TKey)e.Key]));
                    break;

                case SharedDictionaryEvent.SharedDictionaryOperation.SET:
                    Dictionary[(TKey)e.Key] = (TValue)e.Value;
                    break;

                case SharedDictionaryEvent.SharedDictionaryOperation.COUNT:
                    Send(e.Sender, new SharedDictionaryResponseEvent<int>(Dictionary.Count));
                    break;

                case SharedDictionaryEvent.SharedDictionaryOperation.TRYREMOVE:
                    if (Dictionary.ContainsKey((TKey)e.Key))
                    {
                        var Value = Dictionary[(TKey)e.Key];
                        Dictionary.Remove((TKey)e.Key);
                        Send(e.Sender, new SharedDictionaryResponseEvent<Tuple<bool, TValue>>(Tuple.Create(true, Value)));
                    }
                    else
                    {
                        Send(e.Sender, new SharedDictionaryResponseEvent<Tuple<bool, TValue>>(Tuple.Create(false, default(TValue))));
                    }

                    break;
            }
        }
    }
}
