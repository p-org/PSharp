// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Microsoft.PSharp.Runtime;
using Microsoft.PSharp.TestingServices.Runtime;
using Microsoft.PSharp.TestingServices.Scheduling;

namespace Microsoft.PSharp.TestingServices.Specifications
{
    /// <summary>
    /// Checks specifications for correctness during systematic testing.
    /// </summary>
    internal sealed class SpecificationChecker : Specification.Checker
    {
        /// <summary>
        /// The testing runtime that is checking the specifications.
        /// </summary>
        internal SystematicTestingRuntime Runtime;

        /// <summary>
        /// Initializes a new instance of the <see cref="SpecificationChecker"/> class.
        /// </summary>
        internal SpecificationChecker(SystematicTestingRuntime runtime)
            : base()
        {
            this.Runtime = runtime;
        }

        /// <summary>
        /// Checks if the predicate holds, and if not, fails the assertion.
        /// </summary>
        internal override void Assert(bool predicate, string s, object arg0) =>
            this.Runtime.Assert(predicate, s, arg0);

        /// <summary>
        /// Checks if the predicate holds, and if not, fails the assertion.
        /// </summary>
        internal override void Assert(bool predicate, string s, object arg0, object arg1) =>
            this.Runtime.Assert(predicate, s, arg0, arg1);

        /// <summary>
        /// Checks if the predicate holds, and if not, fails the assertion.
        /// </summary>
        internal override void Assert(bool predicate, string s, object arg0, object arg1, object arg2) =>
            this.Runtime.Assert(predicate, s, arg0, arg1, arg2);

        /// <summary>
        /// Checks if the predicate holds, and if not, fails the assertion.
        /// </summary>
        internal override void Assert(bool predicate, string s, params object[] args) =>
            this.Runtime.Assert(predicate, s, args);

        /// <summary>
        /// Returns a nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        internal override bool GetNondeterministicBooleanChoice(int maxValue) =>
            this.Runtime.GetNondeterministicBooleanChoice(null, maxValue);

        /// <summary>
        /// Returns a nondeterministic integer, that can be
        /// controlled during analysis or testing.
        /// </summary>
        internal override int GetNondeterministicIntegerChoice(int maxValue) =>
            this.Runtime.GetNondeterministicIntegerChoice(null, maxValue);

        /// <summary>
        /// Injects a context switch point that can be systematically explored during testing.
        /// </summary>
        internal override void ExploreContextSwitch()
        {
            AsyncMachine caller = this.Runtime.GetExecutingMachine<AsyncMachine>();
            if (caller != null)
            {
                this.Runtime.Scheduler.ScheduleNextOperation(AsyncOperationType.Default,
                    AsyncOperationTarget.Task, caller.Id.Value);
            }
        }

        /// <summary>
        /// Registers a new safety or liveness monitor.
        /// </summary>
        internal override void RegisterMonitor<T>() => this.Runtime.RegisterMonitor(typeof(T));

        /// <summary>
        /// Invokes the specified monitor with the given event.
        /// </summary>
        internal override void Monitor<T>(Event e) => this.Runtime.Monitor(typeof(T), null, e);
    }
}
