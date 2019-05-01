// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.PSharp.Runtime;
using Microsoft.PSharp.TestingServices.Runtime;
using Microsoft.PSharp.TestingServices.Scheduling;
using Microsoft.PSharp.Threading;

namespace Microsoft.PSharp.TestingServices.Threading
{
    /// <summary>
    /// A <see cref="MachineLock"/> that is controlled by the runtime scheduler.
    /// </summary>
    internal sealed class ControlledMachineLock : MachineLock, ISynchronizedResource
    {
        /// <summary>
        /// The testing runtime controlling this lock.
        /// </summary>
        private readonly SystematicTestingRuntime Runtime;

        /// <summary>
        /// Queue of operations awaiting to acquire the lock.
        /// </summary>
        private readonly Queue<MachineOperation> Awaiters;

        /// <summary>
        /// True if the resource has been acquired, else false.
        /// </summary>
        bool ISynchronizedResource.IsAcquired => this.IsAcquired;

        /// <summary>
        /// Initializes a new instance of the <see cref="ControlledMachineLock"/> class.
        /// </summary>
        internal ControlledMachineLock(SystematicTestingRuntime runtime, ulong id)
            : base(id)
        {
            this.Runtime = runtime;
            this.Awaiters = new Queue<MachineOperation>();
        }

        /// <summary>
        /// Tries to acquire the lock asynchronously, and returns a task that completes
        /// when the lock has been acquired. The returned task contains a releaser that
        /// releases the lock when disposed.
        /// </summary>
        public override MachineTask<Releaser> AcquireAsync()
        {
            AsyncMachine caller = this.Runtime.GetExecutingMachine<AsyncMachine>();

            if (this.IsAcquired)
            {
                this.Runtime.Logger.WriteLine("<SyncLog> Machine '{0}' is waiting to acquire lock '{1}'.",
                    caller.Id, this.Id);
                MachineOperation callerOp = this.Runtime.GetAsynchronousOperation(caller.Id.Value);
                this.Awaiters.Enqueue(callerOp);
                callerOp.Status = AsyncOperationStatus.BlockedOnResource;
            }

            this.IsAcquired = true;
            this.Runtime.Logger.WriteLine("<SyncLog> Machine '{0}' is acquiring lock '{1}'.", caller.Id, this.Id);
            this.Runtime.Scheduler.ScheduleNextOperation(AsyncOperationType.Acquire,
                AsyncOperationTarget.Task, caller.Id.Value);

            return MachineTask.FromResult(new Releaser(this));
        }

        /// <summary>
        /// Releases the lock.
        /// </summary>
        protected override void Release()
        {
            AsyncMachine caller = this.Runtime.GetExecutingMachine<AsyncMachine>();
            this.IsAcquired = false;
            if (this.Awaiters.Count > 0)
            {
                MachineOperation awaiterOp = this.Awaiters.Dequeue();
                awaiterOp.Status = AsyncOperationStatus.Enabled;
            }

            this.Runtime.Logger.WriteLine("<SyncLog> Machine '{0}' is releasing lock '{1}'.", caller.Id, this.Id);
            this.Runtime.Scheduler.ScheduleNextOperation(AsyncOperationType.Release,
                AsyncOperationTarget.Task, caller.Id.Value);
        }
    }
}
