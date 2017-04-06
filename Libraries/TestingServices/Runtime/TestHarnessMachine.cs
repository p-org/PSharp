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

        #endregion
    }
}
