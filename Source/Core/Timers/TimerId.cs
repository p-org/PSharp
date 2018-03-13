//-----------------------------------------------------------------------
// <copyright file="TimerId.cs">
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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PSharp.Timers
{
    /// <summary>
    /// Unique identifier for a timer 
    /// </summary>
    public class TimerId 
    {
        /// <summary>
        /// The timer machine id
        /// </summary>
        public readonly MachineId mid;

        /// <summary>
        /// Payload
        /// </summary>
        public readonly object Payload;

        /// <summary>
        /// Initializes a timer id
        /// </summary>
        /// <param name="mid">MachineId</param>
        /// <param name="Payload">Payload</param>
        public TimerId(MachineId mid, object Payload)
        {
            this.mid = mid;
            this.Payload = Payload;
        }

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

            var tid = obj as TimerId;
            if (tid == null)
            {
                return false;
            }

            return mid == tid.mid;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>int</returns>
        public override int GetHashCode()
        {
            return mid.GetHashCode();
        }

        /// <summary>
        /// Returns a string that represents the current timer id.
        /// </summary>
        /// <returns>string</returns>
        public override string ToString()
        {
            return string.Format("Timer[{0},{1}]", mid, Payload != null ? Payload.ToString() : "null");
        }

    }
}
