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

using Microsoft.PSharp.Runtime;

namespace Microsoft.PSharp.TestingServices.Runtime
{
    /// <summary>
    /// The P# test harness machine. This is the root machine
    /// that executes a test method during testing.
    /// </summary>
    internal sealed class TestHarnessMachine
    {
        /// <summary>
        /// The runtime manager that executes this machine.
        /// </summary>
        private IRuntimeMachineManager RuntimeManager;

        /// <summary>
        /// The runtime that executes the test.
        /// </summary>
        private IMachineRuntime Runtime;

        /// <summary>
        /// The unique machine id.
        /// </summary>
        private MachineId Id;

        /// <summary>
        /// The test method.
        /// </summary>
        private readonly MethodInfo TestMethod;

        /// <summary>
        /// The test action.
        /// </summary>
        private readonly Action<IMachineRuntime> TestAction;

        /// <summary>
        /// Stores machine-related information, which can used
        /// for scheduling and testing.
        /// </summary>
        internal MachineInfo Info { get; private set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="testMethod">MethodInfo</param>
        internal TestHarnessMachine(MethodInfo testMethod)
        {
            this.TestMethod = testMethod;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="testAction">Action</param>
        internal TestHarnessMachine(Action<IMachineRuntime> testAction)
        {
            this.TestAction = testAction;
        }

        /// <summary>
        /// Initializes this machine.
        /// </summary>
        /// <param name="runtimeManager">The runtime machine manager.</param>
        /// <param name="runtime">The runtime that executes the test.</param>
        /// <param name="mid">MachineId</param>
        /// <param name="info">MachineInfo</param>
        internal void Initialize(IRuntimeMachineManager runtimeManager, IMachineRuntime runtime, MachineId mid, MachineInfo info)
        {
            this.RuntimeManager = runtimeManager;
            this.Runtime = runtime;
            this.Id = mid;
            this.Info = info;
        }

        /// <summary>
        /// Runs the test harness.
        /// </summary>
        internal void Run()
        {
            try
            {
                // Starts the test.
                if (this.TestAction != null)
                {
                    this.RuntimeManager.Logger.WriteLine("<TestHarnessLog> Running anonymous test method.");
                    this.TestAction(this.Runtime);
                }
                else
                {
                    this.RuntimeManager.Logger.WriteLine("<TestHarnessLog> Running test method " +
                        $"'{this.TestMethod.DeclaringType}.{this.TestMethod.Name}'.");
                    this.TestMethod.Invoke(null, new object[] { this.Runtime });
                }
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException;
            }
        }

        /// <summary>
        /// Wraps the unhandled exception inside an <see cref="AssertionFailureException"/>
        /// exception, and throws it to the user.
        /// </summary>
        /// <param name="ex">Exception</param>
        internal void ReportUnhandledException(Exception ex)
        {
            if (this.TestAction != null)
            {
                this.RuntimeManager.WrapAndThrowException(ex, $"Exception '{ex.GetType()}' was thrown " +
                    $"in anonymous test method, " +
                    $"'{ex.Source}':\n" +
                    $"   {ex.Message}\n" +
                    $"The stack trace is:\n{ex.StackTrace}");
            }
            else
            {
                this.RuntimeManager.WrapAndThrowException(ex, $"Exception '{ex.GetType()}' was thrown " +
                    $"in test method '{this.TestMethod.DeclaringType}.{this.TestMethod.Name}', " +
                    $"'{ex.Source}':\n" +
                    $"   {ex.Message}\n" +
                    $"The stack trace is:\n{ex.StackTrace}");
            }
        }
    }
}
