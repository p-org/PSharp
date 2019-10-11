// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Contains all failure domains
    /// </summary>
    public class FailureDomain
    {
        /// <summary>
        /// Boolean to store the status of domain failure
        /// </summary>
        internal bool DomainFailure;

        /// <summary>
        /// Initializes a new instance of the <see cref="FailureDomain"/> class.
        /// </summary>
        public FailureDomain()
        {
            this.DomainFailure = false;
        }

        /// <summary>
        /// Returns failure domain status
        /// </summary>
        internal bool IsDomainFailed()
        {
            return this.DomainFailure;
        }

        /// <summary>
        /// To trigger failure domain
        /// </summary>
        internal void TriggerDomainFailure()
        {
            this.DomainFailure = true;
        }
    }
}
