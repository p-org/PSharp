// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.ExtendedReflection.ComponentModel;

namespace Microsoft.PSharp.Monitoring.ComponentModel
{
    /// <summary>
    /// Services available in P# cop components
    /// </summary>
    internal interface ICopComponentServices : IComponentServices
    {

        /// <summary>
        /// Gets the monitor manager.
        /// </summary>
        /// <value>The monitor manager.</value>
        IMonitorManager MonitorManager { get; }
    }
}
