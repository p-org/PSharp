//-----------------------------------------------------------------------
// <copyright file="MachineActionTrace.cs">
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
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.PSharp.TestingServices.Tracing.Machines
{
    /// <summary>
    /// Class implementing a P# machine action
    /// trace. It includes all actions that the
    /// machine performs during its execution.
    /// </summary>
    [DataContract]
    public sealed class MachineActionTrace : IEnumerable, IEnumerable<MachineActionInfo>
    {
        #region fields

        /// <summary>
        /// The id of the machine being traced.
        /// </summary>
        private MachineId MachineId;

        /// <summary>
        /// The action infos of the trace.
        /// </summary>
        [DataMember]
        private List<MachineActionInfo> ActionInfos;

        /// <summary>
        /// The send id counter.
        /// </summary>
        private int SendIdCounter;

        /// <summary>
        /// The action id counter.
        /// </summary>
        private int ActionIdCounter;

        /// <summary>
        /// The number of action infos in the trace.
        /// </summary>
        public int Count
        {
            get { return this.ActionInfos.Count; }
        }

        /// <summary>
        /// Index for the machine action trace.
        /// </summary>
        /// <param name="index">Index</param>
        /// <returns>MachineActionInfo</returns>
        public MachineActionInfo this[int index]
        {
            get { return this.ActionInfos[index]; }
            set { this.ActionInfos[index] = value; }
        }

        #endregion

        #region internal API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="mid">MachineId</param>
        /// <param name="isTaskMachine">bool</param>
        internal MachineActionTrace(MachineId mid, bool isTaskMachine = false)
        {
            this.MachineId = mid;
            this.ActionInfos = new List<MachineActionInfo>();
            this.SendIdCounter = 1;
            this.ActionIdCounter = 1;
        }

        /// <summary>
        /// Adds a send action info.
        /// </summary>
        /// <param name="targetMachineId">Target MachineId</param>
        /// <param name="e">Event</param>
        internal void AddSendActionInfo(MachineId targetMachineId, Event e)
        {
            var info = MachineActionInfo.CreateSendActionInfo(this.Count, this.MachineId,
                targetMachineId, e, this.SendIdCounter);
            this.SendIdCounter++;
            this.Push(info);
        }

        /// <summary>
        /// Adds a task machine creation info.
        /// </summary>
        /// <param name="targetMachineId">Task MachineId</param>
        /// <param name="taskId">int</param>
        internal void AddTaskMachineCreationInfo(int taskId, MachineId targetMachineId)
        {
            var info = MachineActionInfo.CreateTaskCreationInfo(this.Count, this.MachineId,
                taskId, targetMachineId);
            this.Push(info);
        }

        /// <summary>
        /// Adds an invocation action info.
        /// </summary>
        /// <param name="actionName">Action name</param>
        /// <param name="receivedEvent">Event</param>
        internal void AddInvocationActionInfo(string actionName, Event receivedEvent)
        {
            var info = MachineActionInfo.CreateInvocationActionInfo(this.Count, this.MachineId,
                actionName, this.ActionIdCounter, receivedEvent);
            this.ActionIdCounter++;
            this.Push(info);
        }

        /// <summary>
        /// Adds a machine creation info.
        /// </summary>
        /// <param name="mid">MachineId</param>
        internal void AddCreateMachineInfo(MachineId mid)
        {
            var info = MachineActionInfo.CreateMachineCreationInfo(this.Count, this.MachineId.Value, mid);
            this.Push(info);
        }

        /// <summary>
        /// Returns the latest machine action info and
        /// removes it from the trace.
        /// </summary>
        /// <returns>MachineActionInfo</returns>
        public MachineActionInfo Pop()
        {
            if (this.Count > 0)
            {
                this.ActionInfos[this.Count - 1].Next = null;
            }

            var info = this.ActionInfos[this.Count - 1];
            this.ActionInfos.RemoveAt(this.Count - 1);

            return info;
        }

        /// <summary>
        /// Returns the latest machine action info
        /// without removing it.
        /// </summary>
        /// <returns>MachineActionInfo</returns>
        public MachineActionInfo Peek()
        {
            MachineActionInfo info = null;

            if (this.ActionInfos.Count > 0)
            {
                info = this.ActionInfos[this.Count - 1];
            }
            
            return info;
        }

        /// <summary>
        /// Returns an enumerator.
        /// </summary>
        /// <returns>IEnumerator</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.ActionInfos.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator.
        /// </summary>
        /// <returns>IEnumerator</returns>
        IEnumerator<MachineActionInfo> IEnumerable<MachineActionInfo>.GetEnumerator()
        {
            return this.ActionInfos.GetEnumerator();
        }

        #endregion

        #region private methods

        /// <summary>
        /// Pushes a new action info to the trace.
        /// </summary>
        /// <param name="actionInfo">MachineActionInfo</param>
        private void Push(MachineActionInfo actionInfo)
        {
            if (this.Count > 0)
            {
                this.ActionInfos[this.Count - 1].Next = actionInfo;
                actionInfo.Previous = this.ActionInfos[this.Count - 1];
            }

            this.ActionInfos.Add(actionInfo);
        }

        #endregion
    }
}
