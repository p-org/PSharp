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

namespace EREngine.ComponentModel
{
    /// <summary>
    /// A ChessCop component
    /// </summary>
    internal interface ICopComponent : IComponent
    {
        /// <summary>
        /// Gets the services.
        /// </summary>
        /// <value>The services.</value>
        new ICopComponentServices Services { get; }
    }
}
