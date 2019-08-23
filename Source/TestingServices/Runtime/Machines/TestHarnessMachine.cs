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
        internal readonly string TestName;

        /// <summary>
        /// Id used to identify subsequent operations performed by this machine.
        /// </summary>
        protected internal override Guid OperationGroupId { get; set; }

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
        /// Runs the test harness asynchronously.
        /// </summary>
        internal Task RunAsync()
        {
            this.Logger.WriteLine($"<TestHarnessLog> Running {this.TestName}.");

            try
            {
                if (this.TestAction != null)
                {
                    this.TestAction(this.Id.Runtime);
                    return Task.CompletedTask;
                }
                else
                {
                    return this.TestFunction(this.Id.Runtime);
                }
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException;
            }
        }
    }
}
