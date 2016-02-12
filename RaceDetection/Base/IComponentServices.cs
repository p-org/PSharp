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
using Microsoft.ExtendedReflection.ComponentModel;
using Microsoft.ExtendedReflection.XmlDocumentation;

namespace EREngine.ComponentModel
{
    /// <summary>
    /// Services available in ChessCop components
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
