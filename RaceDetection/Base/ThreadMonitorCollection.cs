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
using Microsoft.ExtendedReflection.Collections;

namespace EREngine.CallsOnly
{
    /// <summary>
    /// A collection of <see cref="IThreadMonitor"/>
    /// </summary>
    internal sealed class ThreadMonitorCollection : SafeList<IThreadMonitor>
    { }
}
