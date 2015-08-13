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
        /// for executing this task machine.
        /// </summary>
        internal TaskMachineScheduler TaskScheduler;

        /// <summary>
        /// The wrapped task to execute.
        /// </summary>
        internal Task WrappedTask;

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

        #region public static methods

        /// <summary>
        /// Waits for all of the provided task objects to complete execution.
        /// </summary>
        /// <param name="tasks">Tasks</param>
        public static void WaitAll(params Task[] tasks)
        {
            Console.WriteLine("[WAIT]");
            Machine.Dispatcher.ScheduleOnWait(tasks, true);
            Task.WaitAll(tasks);
        }

        /// <summary>
        /// Waits for all of the provided cancellable task objects to complete execution.
        /// </summary>
        /// <param name="tasks">Tasks</param>
        /// <param name="cancellationToken">CancellationToken</param>
        public static void WaitAll(Task[] tasks, CancellationToken cancellationToken)
        {
            Machine.Dispatcher.ScheduleOnWait(tasks, true);
            Task.WaitAll(tasks, cancellationToken);
        }

        /// <summary>
        /// Waits for all of the provided task objects to complete execution
        /// within a specified number of milliseconds.
        /// </summary>
        /// <param name="tasks">Tasks</param>
        /// <param name="millisecondsTimeout">Timeout</param>
        public static void WaitAll(Task[] tasks, int millisecondsTimeout)
        {
            Machine.Dispatcher.ScheduleOnWait(tasks, true);
            Task.WaitAll(tasks, millisecondsTimeout);
        }

        /// <summary>
        /// Waits for all of the provided cancellable task objects to complete
        /// execution within a specified time interval.
        /// </summary>
        /// <param name="tasks">Tasks</param>
        /// <param name="timeout">Timeout</param>
        public static void WaitAll(Task[] tasks, TimeSpan timeout)
        {
            Machine.Dispatcher.ScheduleOnWait(tasks, true);
            Task.WaitAll(tasks, timeout);
        }

        /// <summary>
        /// Waits for all of the provided cancellable task objects to complete
        /// execution within a specified number of milliseconds.
        /// </summary>
        /// <param name="tasks">Tasks</param>
        /// <param name="millisecondsTimeout">Timeout</param>
        /// <param name="cancellationToken">CancellationToken</param>
        public static void WaitAll(Task[] tasks, int millisecondsTimeout,
            CancellationToken cancellationToken)
        {
            Machine.Dispatcher.ScheduleOnWait(tasks, true);
            Task.WaitAll(tasks, millisecondsTimeout, cancellationToken);
        }

        #endregion
    }
}
