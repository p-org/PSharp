//-----------------------------------------------------------------------
// <copyright file="TaskMachine.cs">
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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.TestingServices.Threading
{
    /// <summary>
    /// Class implementing a P# task machine.
    /// </summary>
    public sealed class TaskMachine : AbstractMachine
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

        #region code coverage methods

        /// <summary>
        /// Returns the set of all states in the machine
        /// (for code coverage).
        /// </summary>
        /// <returns>Set of all states in the machine</returns>
        internal override HashSet<string> GetAllStates()
        {
            return new HashSet<string> { "init" };
        }

        /// <summary>
        /// Returns the set of all (states, registered event) pairs in the machine
        /// (for code coverage).
        /// </summary>
        /// <returns>Set of all (states, registered event) pairs in the machine</returns>
        internal override HashSet<Tuple<string, string>> GetAllStateEventPairs()
        {
            return new HashSet<Tuple<string, string>>();
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
        /// <param name="this">Task</param>
        /// <returns>Task</returns>
        public static Task<TResult> RunOnPSharpScheduler<TResult>(this Task<TResult> @this)
        {
            IO.PrintLine("[RunOnPSharpScheduler]");
            return @this.ContinueWith(val => val,
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskMachineExtensions.TaskScheduler).Unwrap();
        }
    }
}
