// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Microsoft.PSharp.Runtime
{
    /// <summary>
    /// Factory for creating machines.
    /// </summary>
    internal static class MachineFactory
    {
        /// <summary>
        /// Cache storing machine constructors.
        /// </summary>
        private static Dictionary<Type, Func<Machine>> MachineConstructorCache;

        /// <summary>
        /// Static constructor.
        /// </summary>
        static MachineFactory()
        {
            MachineConstructorCache = new Dictionary<Type, Func<Machine>>();
        }

        /// <summary>
        /// Creates a new <see cref="Machine"/> of the specified type.
        /// </summary>
        /// <param name="type">Type of the machine.</param>
        /// <returns>The created machine.</returns>
        public static Machine Create(Type type)
        {
            lock (MachineConstructorCache)
            {
                if (!MachineConstructorCache.TryGetValue(type, out Func<Machine> constructor))
                {
                    constructor = Expression.Lambda<Func<Machine>>(
                        Expression.New(type.GetConstructor(Type.EmptyTypes))).Compile();
                    MachineConstructorCache.Add(type, constructor);
                }

                return constructor();
            }
        }

        /// <summary>
        /// Checks if the constructor of the specified machine type exists in the cache.
        /// </summary>
        /// <param name="type">Type of the machine.</param>
        /// <returns>True if the constructor exists, else false.</returns>
        internal static bool IsCached(Type type)
        {
            lock (MachineConstructorCache)
            {
                return MachineConstructorCache.ContainsKey(type);
            }
        }
    }
}
