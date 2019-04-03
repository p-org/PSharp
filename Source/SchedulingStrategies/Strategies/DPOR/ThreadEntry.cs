// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.PSharp.TestingServices.SchedulingStrategies.DPOR
{
    /// <summary>
    /// Thread entry stored on the stack of a depth-first search to track which threads existed
    /// and whether they have been executed already, etc.
    /// </summary>
    internal class ThreadEntry
    {
        /// <summary>
        /// The id/index of this thread in the original thread creation order list of threads.
        /// </summary>
        public int Id;

        /// <summary>
        /// Is the thread enabled?
        /// </summary>
        public bool Enabled;

        /// <summary>
        /// Skip exploring this thread from here.
        /// </summary>
        public bool Sleep;

        /// <summary>
        /// Backtrack to this transition?
        /// </summary>
        public bool Backtrack;

        /// <summary>
        /// Operation type.
        /// </summary>
        public OperationType OpType;

        /// <summary>
        /// Target type. E.g. thread, queue, mutex, variable.
        /// </summary>
        public OperationTargetType TargetType;

        /// <summary>
        /// Target of the operation.
        /// </summary>
        public int TargetId;

        /// <summary>
        /// For a receive operation: the step of the corresponding send.
        /// </summary>
        public int SendStepIndex;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadEntry"/> class.
        /// </summary>
        public ThreadEntry(int id, bool enabled, OperationType opType, OperationTargetType targetType, int targetId, int sendStepIndex)
        {
            this.Id = id;
            this.Enabled = enabled;
            this.Sleep = false;
            this.Backtrack = false;
            this.OpType = opType;
            this.TargetType = targetType;
            this.TargetId = targetId;
            this.SendStepIndex = sendStepIndex;
        }

        internal static readonly Comparer ComparerSingleton = new Comparer();

        internal class Comparer : IEqualityComparer<ThreadEntry>
        {
            public bool Equals(ThreadEntry x, ThreadEntry y) =>
                x.OpType == y.OpType &&
                (x.OpType == OperationType.Yield || x.Enabled == y.Enabled) &&
                x.Id == y.Id &&
                x.TargetId == y.TargetId &&
                x.TargetType == y.TargetType;

            public int GetHashCode(ThreadEntry obj)
            {
                unchecked
                {
                    int hash = 17;
                    hash = (hash * 23) + obj.Id.GetHashCode();
                    hash = (hash * 23) + obj.OpType.GetHashCode();
                    hash = (hash * 23) + obj.TargetId.GetHashCode();
                    hash = (hash * 23) + obj.TargetType.GetHashCode();
                    hash = (hash * 23) + (obj.OpType == OperationType.Yield ? true.GetHashCode() : obj.Enabled.GetHashCode());
                    return hash;
                }
            }
        }
    }
}
