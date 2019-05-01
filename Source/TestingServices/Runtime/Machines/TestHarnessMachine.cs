// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.PSharp.Runtime;

namespace Microsoft.PSharp.TestingServices.Runtime
{
    /// <summary>
    /// Implements a test harness machine that executes the synchronous
    /// test entry point during systematic testing.
    /// </summary>
    internal sealed class TestHarnessMachine : AsyncMachine
    {
        /// <summary>
        /// The test action.
        /// </summary>
        private readonly Action<IMachineRuntime> TestAction;

        /// <summary>
        /// The test function.
        /// </summary>
        private readonly Func<IMachineRuntime, Task> TestFunction;

        /// <summary>
        /// The test name.
        /// </summary>
        private readonly string TestName;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestHarnessMachine"/> class.
        /// </summary>
        internal TestHarnessMachine(Action<IMachineRuntime> testAction, string testName)
            : this(testName)
        {
            this.TestAction = testAction;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestHarnessMachine"/> class.
        /// </summary>
        internal TestHarnessMachine(Func<IMachineRuntime, Task> testFunction, string testName)
            : this(testName)
        {
            this.TestFunction = testFunction;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestHarnessMachine"/> class.
        /// </summary>
        private TestHarnessMachine(string testName)
        {
            this.TestName = string.IsNullOrEmpty(testName) ? "anonymous test" : $"test '{testName}'";
        }

        /// <summary>
        /// Runs the test harness.
        /// </summary>
        internal void Run()
        {
            this.Runtime.Log($"<TestHarnessLog> Running {this.TestName}.");

            try
            {
                this.TestAction(this.Id.Runtime);
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException;
            }
        }

        /// <summary>
        /// Runs the test harness asynchronously.
        /// </summary>
        internal Task RunAsync()
        {
            this.Runtime.Log($"<TestHarnessLog> Running {this.TestName}.");

            try
            {
                return this.TestFunction(this.Id.Runtime);
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
        internal void ReportUnhandledException(Exception ex)
        {
            this.Runtime.WrapAndThrowException(ex, $"Exception '{ex.GetType()}' was thrown " +
                $"in {this.TestName}, " +
                $"'{ex.Source}':\n" +
                $"   {ex.Message}\n" +
                $"The stack trace is:\n{ex.StackTrace}");
        }
    }
}
