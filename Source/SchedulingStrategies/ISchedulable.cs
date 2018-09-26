// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.PSharp.TestingServices.SchedulingStrategies
{
    /// <summary>
    /// Interface of an entity that can be scheduled.
    /// </summary>
    public interface ISchedulable
    {
        /// <summary>
        /// Unique id of the entity.
        /// </summary>
        ulong Id { get; }

        /// <summary>
        /// Name of the entity.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Is the entity enabled.
        /// </summary>
        bool IsEnabled { get; set; }

        /// <summary>
        /// Type of the next operation of the entity.
        /// </summary>
        OperationType NextOperationType { get; }

        /// <summary>
        /// The target type of the next operation of the entity.
        /// </summary>
        OperationTargetType NextTargetType { get; }

        /// <summary>
        /// Target id of the next operation of the entity.
        /// </summary>
        ulong NextTargetId { get; }

        /// <summary>
        /// If the next operation is <see cref="OperationType.Receive"/>
        /// then this gives the step index of the corresponding Send. 
        /// </summary>
        ulong NextOperationMatchingSendIndex { get; }

        /// <summary>
        /// Monotonically increasing operation count.
        /// </summary>
        ulong OperationCount { get; }

        /// <summary>
        /// Unique id of the group of operations that is
        /// associated with the next operation.
        /// </summary>
        Guid NextOperationGroupId { get; }
    }
}
