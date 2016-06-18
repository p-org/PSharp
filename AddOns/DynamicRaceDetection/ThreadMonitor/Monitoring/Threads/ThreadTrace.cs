//-----------------------------------------------------------------------
// <copyright file="ThreadTrace.cs">
//      Copyright (c) 2016 Microsoft Corporation. All rights reserved.
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

namespace Microsoft.PSharp.Monitoring
{
    [Serializable]
    public class ThreadTrace
    {
        #region fields

        /// <summary>
        /// The machine id.
        /// </summary>
        public int MachineId;

        /// <summary>
        /// The action name.
        /// </summary>
        public string ActionName;

        /// <summary>
        /// The action id.
        /// </summary>
        public int ActionId;

        /// <summary>
        /// List of accesses.
        /// </summary>
        public List<ActionInstr> Accesses;

        /// <summary>
        /// The iteration.
        /// </summary>
        public string Iteration = null;

        /// <summary>
        /// Is a task.
        /// </summary>
        public bool IsTask;

        /// <summary>
        /// Task id.
        /// </summary>
        public int TaskId;

        #endregion

        #region constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="machineId">Machine id</param>
        /// <returns>ThreadTrace</returns>
        public static ThreadTrace CreateTraceForMachine(int machineId)
        {
            ThreadTrace trace = new ThreadTrace();

            trace.MachineId = machineId;
            trace.IsTask = false;
            trace.TaskId = -1;

            trace.Accesses = new List<ActionInstr>();

            return trace;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="taskId">Task id</param>
        /// <returns>ThreadTrace</returns>
        public static ThreadTrace CreateTraceForTask(int taskId)
        {
            ThreadTrace trace = new ThreadTrace();

            trace.IsTask = true;
            trace.TaskId = taskId;

            trace.Accesses = new List<ActionInstr>();

            return trace;
        }

        #endregion
    }
}
