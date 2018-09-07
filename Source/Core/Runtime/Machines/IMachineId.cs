// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Interface of a unique machine id.
    /// </summary>
    public interface IMachineId : IEquatable<IMachineId>, IComparable<IMachineId>
    {
        /// <summary>
        /// Unique id value.
        /// </summary>
        ulong Value { get; }

        /// <summary>
        /// Type of the machine.
        /// </summary>
        string Type { get; }

        /// <summary>
        /// Name of the machine.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Optional friendly name of the machine.
        /// </summary>
        string FriendlyName { get; }
    }
}
