// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Reflection;

namespace Microsoft.PSharp.TestingServices
{
    /// <summary>
    /// The P# test harness machine. This is the root machine
    /// that executes a test method during bug-finding.
    /// </summary>
    internal sealed class TestHarnessMachine : AbstractMachine
    {
        #region fields

        /// <summary>
        /// The test method.
        /// </summary>
        private MethodInfo TestMethod;

        /// <summary>
        /// The test action.
        /// </summary>
        private Action<PSharpRuntime> TestAction;

        #endregion

        #region constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="testMethod">MethodInfo</param>
        /// <param name="testAction">Action</param>
        internal TestHarnessMachine(MethodInfo testMethod, Action<PSharpRuntime> testAction)
        {
            this.TestMethod = testMethod;
            this.TestAction = testAction;
        }

        #endregion

        #region test harness logic

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
                    base.Runtime.Log("<TestHarnessLog> Running anonymous test method.");
                    this.TestAction(base.Id.Runtime);
                }
                else
                {
                    base.Runtime.Log("<TestHarnessLog> Running test method " +
                        $"'{this.TestMethod.DeclaringType}.{this.TestMethod.Name}'.");
                    this.TestMethod.Invoke(null, new object[] { base.Id.Runtime });
                }
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException;
            }
        }

        #endregion

        #region error checking

        /// <summary>
        /// Wraps the unhandled exception inside an <see cref="AssertionFailureException"/>
        /// exception, and throws it to the user.
        /// </summary>
        /// <param name="ex">Exception</param>
        internal void ReportUnhandledException(Exception ex)
        {
            if (this.TestAction != null)
            {
                base.Runtime.WrapAndThrowException(ex, $"Exception '{ex.GetType()}' was thrown " +
                    $"in anonymous test method, " +
                    $"'{ex.Source}':\n" +
                    $"   {ex.Message}\n" +
                    $"The stack trace is:\n{ex.StackTrace}");
            }
            else
            {
                base.Runtime.WrapAndThrowException(ex, $"Exception '{ex.GetType()}' was thrown " +
                    $"in test method '{this.TestMethod.DeclaringType}.{this.TestMethod.Name}', " +
                    $"'{ex.Source}':\n" +
                    $"   {ex.Message}\n" +
                    $"The stack trace is:\n{ex.StackTrace}");
            }
        }

        #endregion
    }
}
