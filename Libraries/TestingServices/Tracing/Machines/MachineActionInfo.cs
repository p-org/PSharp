//-----------------------------------------------------------------------
// <copyright file="MachineActionInfo.cs">
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

namespace Microsoft.PSharp.TestingServices.Tracing.Machines
{
    /// <summary>
    /// Class implementing a P# machine action info.
    /// </summary>
    [Serializable]
    public class MachineActionInfo
    {
        #region fields

        /// <summary>
        /// The unique index of this action info.
        /// </summary>
        public int Index;

        /// <summary>
        /// The type of this action info.
        /// </summary>
        public MachineActionType Type { get; private set; }

        /// <summary>
        /// The machine id.
        /// </summary>
        public MachineId MachineId { get; private set; }

        /// <summary>
        /// The send target machine id.
        /// </summary>
        public MachineId TargetMachineId { get; private set; }

        /// <summary>
        /// The send event.
        /// </summary>
        public Event Event { get; private set; }

        /// <summary>
        /// The send id.
        /// </summary>
        public int SendId { get; private set; }

        /// <summary>
        /// The action.
        /// </summary>
        public Action Action { get; private set; }

        /// <summary>
        /// The action id.
        /// </summary>
        public int ActionId { get; private set; }

        /// <summary>
        /// Previous action info.
        /// </summary>
        internal MachineActionInfo Previous;

        /// <summary>
        /// Next action info.
        /// </summary>
        internal MachineActionInfo Next;

        #endregion

        #region internal methods

        /// <summary>
        /// Creates a send action info.
        /// </summary>
        /// <param name="index">Index</param>
        /// <param name="mid">MachineId</param>
        /// <param name="targetMachineId">Target MachineId</param>
        /// <param name="e">Event</param>
        /// <param name="sendId">Send id</param>
        /// <returns>MachineActionInfo</returns>
        internal static MachineActionInfo CreateSendActionInfo(int index, MachineId mid,
            MachineId targetMachineId, Event e, int sendId)
        {
            var actionInfo = new MachineActionInfo();

            actionInfo.Index = index;
            actionInfo.Type = MachineActionType.SendAction;

            actionInfo.MachineId = mid;
            actionInfo.TargetMachineId = targetMachineId;
            actionInfo.Event = e;
            actionInfo.SendId = sendId;

            actionInfo.Previous = null;
            actionInfo.Next = null;

            return actionInfo;
        }

        /// <summary>
        /// Creates an invocation action info.
        /// </summary>
        /// <param name="index">Index</param>
        /// <param name="mid">MachineId</param>
        /// <param name="action">Action</param>
        /// <param name="actionId">Action id</param>
        /// <returns>MachineActionInfo</returns>
        internal static MachineActionInfo CreateInvocationActionInfo(int index, MachineId mid,
            Action action, int actionId)
        {
            var actionInfo = new MachineActionInfo();

            actionInfo.Index = index;
            actionInfo.Type = MachineActionType.InvocationAction;

            actionInfo.MachineId = mid;
            actionInfo.Action = action;
            actionInfo.ActionId = actionId;

            actionInfo.Previous = null;
            actionInfo.Next = null;

            return actionInfo;
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

            MachineActionInfo info = obj as MachineActionInfo;
            if (info == null)
            {
                return false;
            }

            return this.Index == info.Index;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>int</returns>
        public override int GetHashCode()
        {
            return this.Index.GetHashCode();
        }

        #endregion
    }
}
