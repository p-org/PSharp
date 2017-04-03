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

namespace Microsoft.PSharp
{
	/// <summary>
	/// Factory for creating P# machines.
	/// </summary>
	internal static class MachineFactory
	{
		#region static fields

		/// <summary>
		/// Cache storing machine constructors.
		/// </summary>
		private static Dictionary<Type, Func<Machine>> MachineConstructorCache;

		#endregion

		#region constructors

		/// <summary>
		/// Static constructor.
		/// </summary>
		static MachineFactory()
		{
			MachineConstructorCache = new Dictionary<Type, Func<Machine>>();
		}

		#endregion

		#region methods

		/// <summary>
		/// Creates a new P# machine of the specified type.
		/// </summary>
		/// <param name="type">Type</param>
		/// <returns>Machine</returns>
		public static Machine Create(Type type)
		{
            lock (MachineConstructorCache)
            {
                Func<Machine> constructor;
                if (!MachineConstructorCache.TryGetValue(type, out constructor))
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
		/// <param name="type">Type</param>
		/// <returns>Boolean</returns>
		internal static bool IsCached(Type type)
		{
            lock (MachineConstructorCache)
            {
                return MachineConstructorCache.ContainsKey(type);
            }
		}

		#endregion
	}
}
