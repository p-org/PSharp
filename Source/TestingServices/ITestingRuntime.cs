//-----------------------------------------------------------------------
// <copyright file="ITestingRuntime.cs">
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
using System.Reflection;

using Microsoft.PSharp.TestingServices.Coverage;
using Microsoft.PSharp.TestingServices.Scheduling;
using Microsoft.PSharp.TestingServices.Tracing.Error;
using Microsoft.PSharp.TestingServices.Tracing.Schedule;

namespace Microsoft.PSharp.TestingServices
{
    /// <summary>
    /// The interface of the P# testing runtime.
    /// </summary>
    public interface ITestingRuntime : IPSharpRuntime
    {
        /// <summary>
        /// The scheduler used to serialize the execution of
        /// the program, and explore schedules to find bugs.
        /// </summary>
        BugFindingScheduler Scheduler { get; }

        /// <summary>
        /// The P# program schedule trace.
        /// </summary>
        ScheduleTrace ScheduleTrace { get; }

        /// <summary>
        /// The bug trace.
        /// </summary>
        BugTrace BugTrace { get; }

        /// <summary>
        /// Data structure containing information
        /// regarding testing coverage.
        /// </summary>
        CoverageInfo CoverageInfo { get; }

        /// <summary>
        /// Runs a test harness that executes the specified test method.
        /// </summary>
        /// <param name="testMethod">The test method.</param>
        void RunTestHarness(MethodInfo testMethod);

        /// <summary>
        /// Runs a test harness that executes the specified test action.
        /// </summary>
        /// <param name="testAction">The test action.</param>
        void RunTestHarness(Action<IPSharpRuntime> testAction);

        /// <summary>
        /// Gets the id of the currently executing machine.
        /// </summary>
        /// <returns>The machine id, or null, if not present.</returns>
        IMachineId GetCurrentMachineId();

        /// <summary>
        /// Gets the currently executing machine.
        /// </summary>
        /// <returns>The machine, or null if not present.</returns>
        Machine GetCurrentMachine();

        /// <summary>
        /// Checks that no monitor is in a hot state upon program termination.
        /// If the program is still running, then this method returns without
        /// performing a check.
        /// </summary>
        void CheckNoMonitorInHotStateAtTermination();

        /// <summary>
        /// Waits until all P# machines have finished execution.
        /// </summary>
        void Wait();
    }
}
