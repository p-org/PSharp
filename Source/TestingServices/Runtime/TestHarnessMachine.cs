// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Reflection;

using Microsoft.PSharp.Runtime;

namespace Microsoft.PSharp.TestingServices
{
    /// <summary>
    /// The P# test harness machine. This is the root machine
    /// that executes a test method during testing.
    /// </summary>
    internal sealed class TestHarnessMachine : BaseMachine
    {
        /// <summary>
        /// The test method.
        /// </summary>
        private readonly MethodInfo TestMethod;

        /// <summary>
        /// The test action.
        /// </summary>
        private readonly Action<IMachineRuntime> TestAction;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestHarnessMachine"/> class.
        /// </summary>
        internal TestHarnessMachine(MethodInfo testMethod, Action<IMachineRuntime> testAction)
        {
            this.TestMethod = testMethod;
            this.TestAction = testAction;
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
                    this.Runtime.Log("<TestHarnessLog> Running anonymous test method.");
                    this.TestAction(this.Id.Runtime);
                }
                else
                {
                    this.Runtime.Log("<TestHarnessLog> Running test method " +
                        $"'{this.TestMethod.DeclaringType}.{this.TestMethod.Name}'.");
                    this.TestMethod.Invoke(null, new object[] { this.Id.Runtime });
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
        internal void ReportUnhandledException(Exception ex)
        {
            if (this.TestAction != null)
            {
                this.Runtime.WrapAndThrowException(ex, $"Exception '{ex.GetType()}' was thrown " +
                    $"in anonymous test method, " +
                    $"'{ex.Source}':\n" +
                    $"   {ex.Message}\n" +
                    $"The stack trace is:\n{ex.StackTrace}");
            }
            else
            {
                this.Runtime.WrapAndThrowException(ex, $"Exception '{ex.GetType()}' was thrown " +
                    $"in test method '{this.TestMethod.DeclaringType}.{this.TestMethod.Name}', " +
                    $"'{ex.Source}':\n" +
                    $"   {ex.Message}\n" +
                    $"The stack trace is:\n{ex.StackTrace}");
            }
        }
    }
}
