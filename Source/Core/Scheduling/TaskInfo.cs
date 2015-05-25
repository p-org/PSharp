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

namespace Microsoft.PSharp.Scheduling
{
    /// <summary>
    /// Class implementing task related information for scheduling purposes.
    /// </summary>
    public sealed class TaskInfo
    {
        /// <summary>
        /// Task Id.
        /// </summary>
        public readonly int Id;

        /// <summary>
        /// The machine this task corresponds to.
        /// </summary>
        internal Machine Machine;

        /// <summary>
        /// Is task enabled.
        /// </summary>
        public bool IsEnabled
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

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="id">TaskId</param>
        /// <param name="machine">Machine</param>
        internal TaskInfo(int id, Machine machine)
        {
            this.Id = id;
            this.Machine = machine;
            this.IsEnabled = true;
            this.IsActive = false;
            this.HasStarted = false;
            this.IsCompleted = false;
        }
    }
}
