//-----------------------------------------------------------------------
// <copyright file="MachineId.cs">
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
using System.Runtime.Serialization;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Unique machine id.
    /// </summary>
    [DataContract]
    public sealed class MachineId
    {
        #region fields

        /// <summary>
        /// The P# runtime that executes the machine with this id.
        /// </summary>
        public readonly PSharpRuntime Runtime;

        /// <summary>
        /// Type of the machine with this id.
        /// </summary>
        [DataMember]
        public readonly string Type;

        /// <summary>
        /// Unique id value.
        /// </summary>
        [DataMember]
        internal readonly int Value;

        /// <summary>
        /// Machine-type-specific id value.
        /// </summary>
        [DataMember]
        internal readonly int MVal;

        /// <summary>
        /// Endpoint.
        /// </summary>
        [DataMember]
        internal readonly string EndPoint;

        #endregion

        #region static fields

        /// <summary>
        /// Monotonically increasing machine id counter.
        /// </summary>
        private static int IdCounter;

        /// <summary>
        /// Id specific to a machine type.
        /// </summary>
        private static Dictionary<Type, int> TypeIdCounter;

        #endregion

        #region internal API

        /// <summary>
        /// Static constructor.
        /// </summary>
        static MachineId()
        {
            MachineId.IdCounter = 0;
            MachineId.TypeIdCounter = new Dictionary<Type, int>();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="type">Type</param>
        /// <param name="runtime">PSharpRuntime</param>
        internal MachineId(Type type, PSharpRuntime runtime)
        {
            this.Runtime = runtime;

            lock (MachineId.TypeIdCounter)
            {
                if (!MachineId.TypeIdCounter.ContainsKey(type))
                {
                    MachineId.TypeIdCounter.Add(type, 0);
                }

                this.Value = MachineId.IdCounter++;
                this.Type = type.Name;
                this.MVal = MachineId.TypeIdCounter[type]++;

                this.EndPoint = this.Runtime.NetworkProvider.GetLocalEndPoint();
            }
        }

        /// <summary>
        /// Resets the machine id counter.
        /// </summary>
        internal static void ResetMachineIDCounter()
        {
            MachineId.IdCounter = 0;
            MachineId.TypeIdCounter.Clear();
        }

        #endregion

        #region generic public and override methods
        
        /// <summary>
        /// Determines whether the specified System.Object is equal
        /// to the current System.Object.
        /// </summary>
        /// <param name="obj">Object</param>
        /// <returns>Boolean</returns>
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            MachineId mid = obj as MachineId;
            if (mid == null)
            {
                return false;
            }

            return this.Value == mid.Value;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>int</returns>
        public override int GetHashCode()
        {
            return this.Value.GetHashCode();
        }

        /// <summary>
        /// Returns a string that represents the current machine id.
        /// </summary>
        /// <returns>string</returns>
        public override string ToString()
        {
            var text = "[" + this.Type + "," + this.MVal + "," +
                this.EndPoint + "]";
            return text;
        }

        #endregion
    }
}
