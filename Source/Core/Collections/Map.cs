//-----------------------------------------------------------------------
// <copyright file="Map.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation. All rights reserved.
// 
//      THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, 
//      EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES 
//      OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
// ----------------------------------------------------------------------------------
//      The example companies, organizations, products, domain names,
//      e-mail addresses, logos, people, places, and events depicted
//      herein are fictitious.  No association with any real company,
//      organization, product, domain name, email address, logo, person,
//      places, or events is intended or should be inferred.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.PSharp.Collections
{
    /// <summary>
    /// Class implementing a map from K to V.
    /// </summary>
    public class Map<K , V> : ICloneable
    {
        /// <summary>
        /// Map from K to V.
        /// </summary>
        internal Dictionary<K, V> Dictionary;

        /// <summary>
        /// Performs indexing.
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>Value</returns>
        public V this[K key]
        {
            get
            {
                return this.Dictionary[key];
            }
            set
            {
                this.Dictionary[key] = value;
            }
        }

        /// <summary>
        /// Gets the number of elements actually contained in the sequence.
        /// </summary>
        public int Count
        {
            get
            {
                return this.Dictionary.Count;
            }
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public Map()
        {
            this.Dictionary = new Dictionary<K, V>();
        }

        /// <summary>
        /// Adds a new key with the specified value in the map.
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        public void Insert(K key, V value)
        {
            this.Dictionary.Add(key, value);
        }

        /// <summary>
        /// Removes the key from the map.
        /// </summary>
        /// <param name="key">Key</param>
        public void RemoveAt(K key)
        {
            this.Dictionary.Remove(key);
        }

        /// <summary>
        /// Clones the map.
        /// </summary>
        /// <returns>Clone</returns>
        public object Clone()
        {
            var clone = new Map<K, V>();

            var keyType = this.Dictionary.GetType().GetGenericArguments()[0];
            var valueType = this.Dictionary.GetType().GetGenericArguments()[1];

            if ((keyType.IsValueType && valueType.IsValueType) ||
                (keyType == typeof(Machine) && valueType.IsValueType) ||
                (keyType.IsValueType && valueType == typeof(Machine)))
            {
                foreach (var kvp in this.Dictionary)
                {
                    clone.Dictionary.Add(kvp.Key, kvp.Value);
                }
            }
            else if (typeof(ICloneable).IsAssignableFrom(keyType) &&
                typeof(ICloneable).IsAssignableFrom(valueType))
            {
                foreach (var kvp in this.Dictionary)
                {
                    var clonedKey = (kvp.Key as ICloneable).Clone();
                    var clonedValue = (kvp.Value as ICloneable).Clone();
                    clone.Dictionary.Add((K)Convert.ChangeType(clonedKey, typeof(K)),
                        (V)Convert.ChangeType(clonedValue, typeof(V)));
                }
            }
            else if (typeof(ICloneable).IsAssignableFrom(keyType) &&
                valueType.IsValueType)
            {
                foreach (var kvp in this.Dictionary)
                {
                    var clonedKey = (kvp.Key as ICloneable).Clone();
                    clone.Dictionary.Add((K)Convert.ChangeType(clonedKey, typeof(K)), kvp.Value);
                }
            }
            else if (keyType.IsValueType &&
                typeof(ICloneable).IsAssignableFrom(valueType))
            {
                foreach (var kvp in this.Dictionary)
                {
                    var clonedValue = (kvp.Value as ICloneable).Clone();
                    clone.Dictionary.Add(kvp.Key, (V)Convert.ChangeType(clonedValue, typeof(V)));
                }
            }
            else
            {
                clone.Dictionary = this.Dictionary;
            }

            return clone;
        }
    }
}
