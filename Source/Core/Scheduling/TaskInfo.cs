//-----------------------------------------------------------------------
// <copyright file="TaskInfo.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation. All rights reserved.
// 
//      THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, 
//      EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES 
//      OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
// ----------------------------------------------------------------------------------
//      The example companies, organizations, products, domain names,
//      e-mail addresses, logos, people, places, and events depicted
//      herein are fictitious.  No association with any real company,
//      organization, product, domain name, email address, logo, person,
//      places, or events is intended or should be inferred.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.PSharp.Scheduling
{
    /// <summary>
    /// Class implementing task related information for scheduling purposes.
    /// </summary>
    public sealed class TaskInfo
    {
        #region fields

        /// <summary>
        /// Task Id.
        /// </summary>
        public readonly int Id;

        /// <summary>
        /// The machine this task corresponds to.
        /// </summary>
        internal BaseMachine Machine;

        /// <summary>
        /// List of wrapped tasks that block this task.
        /// </summary>
        internal List<TaskInfo> BlockingWrappedTasks;

        /// <summary>
        /// List of tasks that block this task.
        /// </summary>
        internal List<Task> BlockingUnwrappedTasks;

        /// <summary>
        /// True if this task should wait all blocking
        /// tasks to complete, before unblocking.
        /// </summary>
        internal bool WaitAll;

        /// <summary>
        /// Is task enabled.
        /// </summary>
        public bool IsEnabled
        {
            get; internal set;
        }

        /// <summary>
        /// Is task waiting to receive an event.
        /// </summary>
        public bool IsWaiting
        {
            get; internal set;
        }

        /// <summary>
        /// Is task blocked.
        /// </summary>
        public bool IsBlocked
        {
            get; internal set;
        }

        /// <summary>
        /// Is task active.
        /// </summary>
        public bool IsActive
        {
            get; internal set;
        }

        /// <summary>
        /// Has the task started.
        /// </summary>
        public bool HasStarted
        {
            get; internal set;
        }

        /// <summary>
        /// Is task completed.
        /// </summary>
        public bool IsCompleted
        {
            get; internal set;
        }

        #endregion

        #region constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="id">TaskId</param>
        /// <param name="machine">Machine</param>
        internal TaskInfo(int id, BaseMachine machine)
        {
            this.Id = id;
            this.Machine = machine;
            this.IsEnabled = true;
            this.IsWaiting = false;
            this.IsBlocked = false;
            this.IsActive = false;
            this.HasStarted = false;
            this.IsCompleted = false;

            this.BlockingWrappedTasks = new List<TaskInfo>();
            this.BlockingUnwrappedTasks = new List<Task>();
            this.WaitAll = false;
        }

        #endregion

        #region generic public and override methods

        /// <summary>
        /// Determines whether the specified System.Object is equal
        /// to the current System.Object.
        /// </summary>
        /// <param name="obj">Object</param>
        /// <returns>Boolean value</returns>
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            TaskInfo mid = obj as TaskInfo;
            if (mid == null)
            {
                return false;
            }

            return this.Id == mid.Id;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>int</returns>
        public override int GetHashCode()
        {
            return this.Id.GetHashCode();
        }

        #endregion
    }
}
