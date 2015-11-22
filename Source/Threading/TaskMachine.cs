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

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.PSharp.Threading
{
    /// <summary>
    /// Class implementing a P# task machine.
    /// </summary>
    public sealed class TaskMachine : BaseMachine
    {
        #region fields

        /// <summary>
        /// The task scheduler that is responsible
        /// for wrapping and executing tasks.
        /// </summary>
        internal TaskWrapperScheduler TaskScheduler;

        /// <summary>
        /// The wrapped task to execute.
        /// </summary>
        internal Task WrappedTask;

        #endregion

        #region machine constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="taskScheduler">TaskScheduler</param>
        /// <param name="task">Task</param>
        internal TaskMachine(TaskWrapperScheduler taskScheduler, Task task)
            : base()
        {
            this.TaskScheduler = taskScheduler;
            this.WrappedTask = task;
        }

        #endregion

        #region internal methods

        /// <summary>
        /// Runs the task machine.
        /// </summary>
        internal void Run()
        {
            this.TaskScheduler.Execute(this.WrappedTask);
            this.WrappedTask.Wait();
        }

        #endregion
    }

    /// <summary>
    /// Class implementing task extensions.
    /// </summary>
    public static class TaskMachineExtensions
    {
        /// <summary>
        /// The task scheduler that is responsible
        /// for wrapping and executing tasks.
        /// </summary>
        internal static TaskWrapperScheduler TaskScheduler;

        /// <summary>
        /// Run the task on the P# task scheduler.
        /// </summary>
        /// <typeparam name="TResult">Task result</typeparam>
        /// <param name="@this">Task</param>
        /// <returns>Task</returns>
        public static Task<TResult> RunOnPSharpScheduler<TResult>(this Task<TResult> @this)
        {
            Console.WriteLine("[RunOnPSharpScheduler]");
            return @this.ContinueWith(val => val,
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskMachineExtensions.TaskScheduler).Unwrap();
        }
    }
}
