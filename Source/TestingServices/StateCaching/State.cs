//-----------------------------------------------------------------------
// <copyright file="State.cs">
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

using System.Collections.Generic;

using Microsoft.PSharp.IO;

namespace Microsoft.PSharp.TestingServices.StateCaching
{
    /// <summary>
    /// Represents a snapshot of the program state.
    /// </summary>
    internal sealed class State
    {
        /// <summary>
        /// The fingerprint of the trace step.
        /// </summary>
        internal Fingerprint Fingerprint { get; private set; }

        /// <summary>
        /// Map from monitors to their liveness status.
        /// </summary>
        internal readonly Dictionary<Monitor, MonitorStatus> MonitorStatus;

        /// <summary>
        /// Ids of the enabled machines. Only relevant
        /// if this is a scheduling trace step.
        /// </summary>
        internal readonly HashSet<ulong> EnabledMachineIds;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="fingerprint">Fingerprint</param>
        /// <param name="enabledMachineIds">Ids of enabled machines</param>
        /// <param name="monitorStatus">Monitor status</param>
        internal State(Fingerprint fingerprint, HashSet<ulong> enabledMachineIds, Dictionary<Monitor, MonitorStatus> monitorStatus)
        {
            this.Fingerprint = fingerprint;
            this.EnabledMachineIds = enabledMachineIds;
            this.MonitorStatus = monitorStatus;
        }

        /// <summary>
        /// Pretty prints the state.
        /// </summary>
        internal void PrettyPrint()
        {
            Debug.WriteLine($"Fingerprint: {this.Fingerprint}");
            foreach (var id in this.EnabledMachineIds)
            {
                Debug.WriteLine($"  Enabled machine id: {id}");
            }

            foreach (var m in this.MonitorStatus)
            {
                Debug.WriteLine($"  Monitor status: {m.Key.Id} is {m.Value}");
            }
        }
    }
}
