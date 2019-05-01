// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.PSharp.Threading
{
    /// <summary>
    /// Implements an awaitable that asynchronously yields back to the current context when awaited.
    /// </summary>
    public readonly struct YieldAwaitable
    {
        /// <summary>
        /// The currently installed <see cref="MachineTask"/> scheduler.
        /// </summary>
        private readonly MachineTaskScheduler Scheduler;

        /// <summary>
        /// Initializes a new instance of the <see cref="YieldAwaitable"/> struct.
        /// </summary>
        internal YieldAwaitable(MachineTaskScheduler scheduler)
        {
            this.Scheduler = scheduler;
        }

        /// <summary>
        /// Gets an awaiter for this awaitable.
        /// </summary>
        public YieldAwaiter GetAwaiter() => this.Scheduler.CreateYieldAwaiter();

        /// <summary>
        /// Provides an awaiter that switches into a target environment.
        /// </summary>
        public readonly struct YieldAwaiter : ICriticalNotifyCompletion, INotifyCompletion
        {
            /// <summary>
            /// The currently installed <see cref="MachineTask"/> scheduler.
            /// </summary>
            private readonly MachineTaskScheduler Scheduler;

            /// <summary>
            /// The internal yield awaiter.
            /// </summary>
            private readonly System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Awaiter;

            /// <summary>
            /// Gets a value that indicates whether a yield is not required.
            /// </summary>
#pragma warning disable CA1822 // Mark members as static
            public bool IsCompleted => false;
#pragma warning restore CA1822 // Mark members as static

            /// <summary>
            /// Initializes a new instance of the <see cref="YieldAwaiter"/> struct.
            /// </summary>
            internal YieldAwaiter(MachineTaskScheduler scheduler)
            {
                this.Scheduler = scheduler;
                this.Awaiter = default;
            }

            /// <summary>
            /// Ends the await operation.
            /// </summary>
            public void GetResult() => this.Scheduler.GetYieldResult(this.Awaiter);

            /// <summary>
            /// Posts the continuation action back to the current context.
            /// </summary>
            public void OnCompleted(Action continuation) => this.Scheduler.OnYieldCompleted(continuation, this.Awaiter);

            /// <summary>
            /// Posts the continuation action back to the current context.
            /// </summary>
            public void UnsafeOnCompleted(Action continuation) => this.Scheduler.UnsafeOnYieldCompleted(continuation, this.Awaiter);
        }
    }
}
