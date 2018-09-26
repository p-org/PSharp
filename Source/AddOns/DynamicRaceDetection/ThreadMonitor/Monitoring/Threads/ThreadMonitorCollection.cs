// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.ExtendedReflection.Collections;

namespace Microsoft.PSharp.Monitoring.CallsOnly
{
    /// <summary>
    /// A collection of thread monitors.
    /// </summary>
    internal sealed class ThreadMonitorCollection : SafeList<IThreadMonitor>
    {

    }
}
