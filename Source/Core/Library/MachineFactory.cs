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
using System.Threading.Tasks;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Factory for creating P# machines.
    /// </summary>
    internal class DefaultMachineFactory : IMachineFactory
    {
        #region fields

        /// <summary>
        /// Cache storing machine constructors.
        /// </summary>
        private Dictionary<Type, Func<Machine>> MachineConstructorCache;

        #endregion

        #region constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        public DefaultMachineFactory()
        {
            MachineConstructorCache = new Dictionary<Type, Func<Machine>>();
        }

        #endregion

        #region methods

        /// <summary>
        /// Types for which this factory is responsible
        /// </summary>
        public Type BaseClassType()
        {
            return typeof(Machine);
        }

        /// <summary>
        /// Creates a new P# machine of the specified type.
        /// </summary>
        /// <param name="type">Type</param>
        /// <param name="runtime">PSharpRuntime</param>
        /// <param name="creator">Creator machine, if any</param>
        /// <param name="mid">Id of the new machine</param>
        /// <param name="info">MachineInfo</param>
        /// <returns>Machine</returns>
        public Machine Create(Type type, PSharpRuntime runtime, AbstractMachine creator, MachineId mid, MachineInfo info)
        {
            Machine newMachine;

            lock (MachineConstructorCache)
            {
                Func<Machine> constructor;
                if (!MachineConstructorCache.TryGetValue(type, out constructor))
                {
                    constructor = Expression.Lambda<Func<Machine>>(
                        Expression.New(type.GetConstructor(Type.EmptyTypes))).Compile();
                    MachineConstructorCache.Add(type, constructor);
                }

                newMachine = constructor();
            }

            newMachine.Initialize(runtime, mid, info);
            newMachine.InitializeStateInformation();

            return newMachine;
        }

        #endregion
    }
}
