//-----------------------------------------------------------------------
// <copyright file="TestingRuntime.cs">
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
using System.Linq;
using System.Threading.Tasks;

using Microsoft.PSharp.Runtime;
using Microsoft.PSharp.TestingServices.Scheduling;
using Microsoft.PSharp.TestingServices.SchedulingStrategies;

namespace Microsoft.PSharp.TestingServices.Runtime
{
    /// <summary>
    /// Runtime for executing machines in bug-finding mode.
    /// </summary>
    internal class TestingRuntime : BaseTestingRuntime, IStateMachineRuntime
    {
        /// <summary>
        /// The base machine types that can execute on this runtime.
        /// </summary>
        private readonly HashSet<Type> SupportedBaseMachineTypes;

        /// <summary>
        /// Creates a P# runtime that executes in bug-finding mode.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="strategy">SchedulingStrategy</param>
        /// <param name="reporter">Reporter to register runtime operations.</param>
        /// <returns>TestingRuntime</returns>
        [TestRuntimeCreate]
        internal static TestingRuntime Create(Configuration configuration, ISchedulingStrategy strategy, IRegisterRuntimeOperation reporter)
        {
            return new TestingRuntime(configuration, strategy, reporter);
        }

        /// <summary>
        /// Returns the type of the bug-finding runtime.
        /// </summary>
        /// <returns></returns>
        [TestRuntimeGetType]
        internal static Type GetRuntimeType() => typeof(IStateMachineRuntime);

        /// <summary>
        /// Constructor.
        /// <param name="configuration">Configuration</param>
        /// <param name="strategy">SchedulingStrategy</param>
        /// <param name="reporter">Reporter to register runtime operations.</param>
        /// </summary>
        protected TestingRuntime(Configuration configuration, ISchedulingStrategy strategy, IRegisterRuntimeOperation reporter)
            : base(configuration, strategy, reporter)
        {
            this.SupportedBaseMachineTypes = new HashSet<Type> { typeof(Machine), typeof(TestHarnessMachine) };
        }

        #region machine creation and execution

        /// <summary>
        /// Creates a new P# machine using the specified unbound <see cref="MachineId"/> and type.
        /// </summary>
        /// <param name="mid">Unbound machine id.</param>
        /// <param name="type">Type of the machine.</param>
        /// <returns>Task that represents the asynchronous operation. The task result is the machine.</returns>
        protected override async Task<IMachine> CreateMachineAsync(MachineId mid, Type type)
        {
            Machine machine = MachineFactory.Create(type);
            await machine.InitializeAsync(this, mid, new SchedulableInfo(mid));
            return machine;
        }

        /// <summary>
        /// Checks if the specified type is a machine that can execute on this runtime.
        /// </summary>
        /// <returns>True if the type is supported, else false.</returns>
        protected override bool IsSupportedMachineType(Type type) =>
            this.SupportedBaseMachineTypes.Any(machineType => type.IsSubclassOf(machineType));

        /// <summary>
        /// Checks if the constructor of the machine constructor for the
        /// specified machine type exists in the cache.
        /// </summary>
        /// <param name="type">Type</param>
        /// <returns>Boolean</returns>
        protected override bool IsMachineConstructorCached(Type type) => MachineFactory.IsCached(type);

        #endregion

        #region specifications and error checking

        /// <summary>
        /// Checks that the specified machine method was invoked properly.
        /// </summary>
        /// <param name="caller">The caller machine.</param>
        /// <param name="method">The invoked machine method.</param>
        protected override void CheckMachineMethodInvocation(IMachine caller, string method)
        {
            if (caller == null)
            {
                return;
            }

            var executingMachine = this.GetCurrentMachine();
            if (executingMachine == null)
            {
                return;
            }

            // Check that the caller is a supported machine type (if it is not the environment).
            this.Assert(this.IsSupportedMachineType(caller.GetType()), "Object '{0}' invoked method '{1}' without being a machine.",
                caller.Id, method);

            // Asserts that the machine calling a P# machine method is also
            // the machine that is currently executing.
            this.Assert(executingMachine.Equals(caller), "Machine '{0}' invoked method '{1}' on behalf of machine '{2}'.",
                executingMachine.Id, method, caller.Id);

            switch (method)
            {
                case MachineApiNames.CreateMachineApiName:
                case MachineApiNames.SendEventApiName:
                    this.AssertNoPendingTransitionStatement(caller, method);
                    break;

                case MachineApiNames.CreateMachineAndExecuteApiName:
                    this.Assert(caller != null, "Only a machine can execute 'CreateMachineAndExecute'. Avoid calling " +
                        "directly from the PSharp Test method. Instead call through a 'harness' machine.");
                    this.Assert(caller is Machine, "Only a machine of type '{0}' can execute 'CreateMachineAndExecute'.",
                        typeof(Machine).FullName);
                    this.AssertNoPendingTransitionStatement(caller, method);
                    break;

                case MachineApiNames.SendEventAndExecuteApiName:
                    this.Assert(caller != null, "Only a machine can execute 'SendEventAndExecute'. Avoid calling " +
                        "directly from the PSharp Test method. Instead call through a 'harness' machine.");
                    this.Assert(caller is Machine, "Only a machine of type '{0}' can execute 'SendEventAndExecute'.",
                        typeof(Machine).FullName);
                    this.AssertNoPendingTransitionStatement(caller, method);
                    break;

                case MachineApiNames.RaiseEventApiName:
                case MachineApiNames.PopStateApiName:
                    this.AssertTransitionStatement(caller);
                    break;

                case MachineApiNames.MonitorEventApiName:
                case MachineApiNames.RandomApiName:
                case MachineApiNames.RandomIntegerApiName:
                case MachineApiNames.FairRandomApiName:
                case MachineApiNames.ReceiveEventApiName:
                    this.AssertNoPendingTransitionStatement(caller, method);
                    break;

                default:
                    this.Assert(false, "Machine '{0}' invoked unexpected method '{1}'.", executingMachine.Id, method);
                    break;
            }
        }

        /// <summary>
        /// Asserts that a transition statement (raise, goto or pop) has not
        /// already been called. Records that RGP has been called.
        /// </summary>
        /// <param name="caller">The caller machine.</param>
        private void AssertTransitionStatement(IMachine caller)
        {
            this.Assert(!caller.Info.IsInsideOnExit, "Machine '{0}' has called raise, goto, push or pop " +
                "inside an OnExit method.", caller.Id.Name);
            this.Assert(!caller.Info.CurrentActionCalledTransitionStatement, "Machine '{0}' has called multiple " +
                "raise, goto, push or pop in the same action.", caller.Id.Name);
            caller.Info.CurrentActionCalledTransitionStatement = true;
        }

        /// <summary>
        /// Asserts that a transition statement (raise, goto or pop)
        /// has not already been called.
        /// </summary>
        /// <param name="caller">The caller machine.</param>
        /// <param name="method">The invoked machine method.</param>
        private void AssertNoPendingTransitionStatement(IMachine caller, string method)
        {
            this.Assert(!caller.Info.CurrentActionCalledTransitionStatement, "Machine '{0}' cannot call '{1}' " +
                "after calling raise, goto, push or pop in the same action.", caller.Id.Name, method);
        }

        #endregion

        #region timers

        /// <summary>
        /// Return the timer machine type
        /// </summary>
        /// <returns></returns>
        public override Type GetTimerMachineType()
        {
            var timerType = base.GetTimerMachineType();
            if (timerType == null)
            {
                return typeof(Timers.ModelTimerMachine);
            }

            return timerType;
        }

        #endregion
    }
}
