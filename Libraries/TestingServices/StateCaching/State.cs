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

namespace Microsoft.PSharp.TestingServices.StateCaching
{
    /// <summary>
    /// Class implementing a P# program state.
    /// </summary>
    internal sealed class State
    {
        #region fields

        /// <summary>
        /// The fingerprint of the trace step.
        /// </summary>
        internal Fingerprint Fingerprint { get; private set; }

        /// <summary>
        /// Map from monitors to their liveness status.
        /// </summary>
        internal Dictionary<Monitor, MonitorStatus> MonitorStatus;

        /// <summary>
        /// The enabled machines. Only relevant if this is a scheduling
        /// trace step.
        /// </summary>
        internal HashSet<AbstractMachine> EnabledMachines;

        #endregion

        #region internal API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="fingerprint">Fingerprint</param>
        /// <param name="enabledMachines">Enabled machines</param>
        /// <param name="monitorStatus">Monitor status</param>
        internal State(Fingerprint fingerprint, HashSet<AbstractMachine> enabledMachines,
            Dictionary<Monitor, MonitorStatus> monitorStatus)
        {
            this.Fingerprint = fingerprint;
            this.EnabledMachines = enabledMachines;
            this.MonitorStatus = monitorStatus;
        }

        #endregion
    }
}
