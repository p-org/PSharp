//-----------------------------------------------------------------------
// <copyright file="TaskMachine.cs" company="Microsoft">
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

using System.Threading.Tasks;

namespace Microsoft.PSharp.Threading
{
    /// <summary>
    /// Class implementing a P# task machine.
    /// </summary>
    internal sealed class TaskMachine : BaseMachine
    {
        #region fields

        /// <summary>
        /// The task scheduler that is responsible
        /// for executing this task machine.
        /// </summary>
        internal TaskMachineScheduler TaskScheduler;

        /// <summary>
        /// The task to execute.
        /// </summary>
        internal Task Task;

        #endregion

        #region machine constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="task">Task</param>
        internal TaskMachine(TaskMachineScheduler taskScheduler, Task task)
            : base()
        {
            this.TaskScheduler = taskScheduler;
            this.Task = task;
        }

        #endregion

        #region P# internal methods

        /// <summary>
        /// Runs the task machine.
        /// </summary>
        internal void Run()
        {
            this.TaskScheduler.Execute(this.Task);
            this.Task.Wait();
        }

        #endregion
    }
}
