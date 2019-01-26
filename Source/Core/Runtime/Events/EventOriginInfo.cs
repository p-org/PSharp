//-----------------------------------------------------------------------
// <copyright file="EventOriginInfo.cs">
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

using System.Runtime.Serialization;

namespace Microsoft.PSharp.Runtime
{
    /// <summary>
    /// Contains origin information regarding an <see cref="Event"/>.
    /// </summary>
    [DataContract]
    internal class EventOriginInfo
    {
        /// <summary>
        /// The sender machine id.
        /// </summary>
        [DataMember]
        internal MachineId SenderMachineId { get; private set; }

        /// <summary>
        /// The sender machine name.
        /// </summary>
        [DataMember]
        internal string SenderMachineName { get; private set; }

        /// <summary>
        /// The sender machine state name.
        /// </summary>
        [DataMember]
        internal string SenderStateName { get; private set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="senderMachineId">Sender machine id</param>
        /// <param name="senderMachineName">Sender machine name</param>
        /// <param name="senderStateName">Sender state name</param>
        internal EventOriginInfo(MachineId senderMachineId, string senderMachineName, string senderStateName)
        {
            this.SenderMachineId = senderMachineId;
            this.SenderMachineName = senderMachineName;
            this.SenderStateName = senderStateName;
        }
    }
}
