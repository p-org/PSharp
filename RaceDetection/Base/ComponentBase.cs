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
    /// Base class for <see cref="IChessCopComponent"/>.
    /// </summary>
    internal class CopComponentBase : ComponentBase, ICopComponent
    {
        ICopComponentServices _services;
        /// <summary>
        /// Gets the services.
        /// </summary>
        /// <value>The services.</value>
        public new ICopComponentServices Services
        {
            get
            {
                if (this._services == null)
                    this._services = new CopComponentServices(this);
                return this._services;
            }
        }
    }
}
