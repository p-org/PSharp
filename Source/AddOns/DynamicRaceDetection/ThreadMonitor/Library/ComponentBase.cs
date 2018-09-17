// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.ExtendedReflection.ComponentModel;

namespace Microsoft.PSharp.Monitoring.ComponentModel
{
    /// <summary>
    /// Base class for P# CopComponentBase.
    /// </summary>
    internal class CopComponentBase : ComponentBase, ICopComponent
    {
        ICopComponentServices _services;

        /// <summary>
        /// Gets the services.
        /// </summary>
        public new ICopComponentServices Services
        {
            get
            {
                if (this._services == null)
                {
                    this._services = new CopComponentServices(this);
                }

                return this._services;
            }
        }
    }
}
