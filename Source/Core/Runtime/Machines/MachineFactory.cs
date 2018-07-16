//-----------------------------------------------------------------------
// <copyright file="MachineFactory.cs">
//      Copyright (c) Microsoft Corporation. All rights reserved.
//
//      THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//      EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//      MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//      IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//      CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//      TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//      SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
//-----------------------------------------------------------------------

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
