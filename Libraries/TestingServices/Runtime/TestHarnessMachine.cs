//-----------------------------------------------------------------------
// <copyright file="TestHarnessMachine.cs">
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

namespace Microsoft.PSharp.TestingServices
{
    /// <summary>
    /// The P# test harness machine. This is the root machine
    /// that executes a test harness during bug-finding.
    /// </summary>
    internal sealed class TestHarnessMachine : Machine
    {
        #region runners

        /// <summary>
        /// Runs the specified test.
        /// </summary>
        /// <param name="runtime">BugFindingRuntime</param>
        /// <param name="test">Test</param>
        internal static void Run(BugFindingRuntime runtime, Action<PSharpRuntime> test)
        {
            Run(runtime, test, null);
        }

        /// <summary>
        /// Runs the specified test.
        /// </summary>
        /// <param name="runtime">BugFindingRuntime</param>
        /// <param name="test">Test</param>
        internal static void Run(BugFindingRuntime runtime, MethodInfo test)
        {
            Run(runtime, null, test);
        }

        /// <summary>
        /// Runs the specified test.
        /// </summary>
        /// <param name="runtime">BugFindingRuntime</param>
        /// <param name="testAction">Action</param>
        /// <param name="testMethod">MethodInfo</param>
        private static void Run(BugFindingRuntime runtime, Action<PSharpRuntime> testAction,
            MethodInfo testMethod)
        {
            MachineId mid = new MachineId(typeof(TestHarnessMachine), null, runtime);
            Machine harness = new TestHarnessMachine();
            harness.SetMachineId(mid);
            harness.InitializeStateInformation();
            harness.GotoStartState(new SetEntryPoint(runtime, testAction, testMethod));
        }

        #endregion

        #region state-machine logic

        /// <summary>
        /// Sets the entry point to the P# program-under-test.
        /// </summary>
        private class SetEntryPoint : Event
        {
            /// <summary>
            /// The bug-finding runtime.
            /// </summary>
            internal BugFindingRuntime Runtime;

            /// <summary>
            /// The test action to execute.
            /// </summary>
            internal Action<PSharpRuntime> TestAction;

            /// <summary>
            /// The test method to execute.
            /// </summary>
            internal MethodInfo TestMethod;

            /// <summary>
            /// Creates a new instance of the event.
            /// </summary>
            /// <param name="runtime">BugFindingRuntime</param>
            /// <param name="testAction">Action</param>
            /// <param name="testMethod">MethodInfo</param>
            internal SetEntryPoint(BugFindingRuntime runtime, Action<PSharpRuntime> testAction,
                MethodInfo testMethod)
            {
                this.Runtime = runtime;
                this.TestAction = testAction;
                this.TestMethod = testMethod;
            }
        }

        [Start]
        [OnEntry(nameof(ExecuteEntryPoint))]
        private class Testing : MachineState { }

        private void ExecuteEntryPoint()
        {
            var runtime = (this.ReceivedEvent as SetEntryPoint).Runtime;
            var testAction = (this.ReceivedEvent as SetEntryPoint).TestAction;
            var testMethod = (this.ReceivedEvent as SetEntryPoint).TestMethod;

            // Starts the test.
            if (testAction != null)
            {
                testAction(runtime);
            }
            else
            {
                testMethod.Invoke(null, new object[] { runtime });
            }
        }
        
        #endregion
    }
}
