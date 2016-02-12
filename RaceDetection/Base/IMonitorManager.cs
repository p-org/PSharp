/********************************************************
*                                                       *
*     Copyright (C) Microsoft. All rights reserved.     *
*                                                       *
********************************************************/

// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.ExtendedReflection.Monitoring;
using Microsoft.ExtendedReflection.ComponentModel;
using EREngine.CallsOnly;

namespace EREngine
{
    /// <summary>
    /// Service to register monitors
    /// </summary>
    internal interface IMonitorManager
        : IService
    {
        /// <summary>
        /// Registers a thread monitor.
        /// </summary>
        /// <param name="threadMonitor">The thread monitor.</param>
        void RegisterThreadMonitor(IThreadMonitor threadMonitor);

        /// <summary>
        /// Registers a thread monitor factory.
        /// </summary>
        /// <param name="monitorFactory">The monitor factory.</param>
        void RegisterThreadMonitorFactory(IThreadMonitorFactory monitorFactory);

        /// <summary>
        /// Registers the memory access thread monitor.
        /// </summary>
        void RegisterObjectAccessThreadMonitor();

        /// <summary>
        /// get rid of accummulated execution monitors
        /// </summary>
        void DisposeExecutionMonitors();
    }
}
