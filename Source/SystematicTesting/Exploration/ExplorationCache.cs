//-----------------------------------------------------------------------
// <copyright file="ExplorationCache.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation. All rights reserved.
// 
//      THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, 
//      EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES 
//      OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
// ----------------------------------------------------------------------------------
//      The example companies, organizations, products, domain names,
//      e-mail addresses, logos, people, places, and events depicted
//      herein are fictitious.  No association with any real company,
//      organization, product, domain name, email address, logo, person,
//      places, or events is intended or should be inferred.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.SystematicTesting.Exploration
{
    /// <summary>
    /// Class implementing a P# exploration cache.
    /// </summary>
    internal sealed class ExplorationCache
    {
        #region fields

        /// <summary>
        /// Configuration.
        /// </summary>
        private Configuration Configuration;

        /// <summary>
        /// Cache of operation delays already explored.
        /// </summary>
        internal List<int> OperationDelaysCache;

        #endregion

        #region public API

        /// <summary>
        /// Creates a new exploration cache.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <returns>ExplorationCache</returns>
        internal static ExplorationCache Create(Configuration configuration)
        {
            return new ExplorationCache(configuration);
        }

        /// <summary>
        /// Resets the exploration cache.
        /// </summary>
        internal void Reset()
        {
            this.OperationDelaysCache = Enumerable.Repeat(0,
                this.Configuration.OperationDelayBound).ToList();
        }

        #endregion

        #region private API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        private ExplorationCache(Configuration configuration)
        {
            this.Configuration = configuration;
            this.OperationDelaysCache = Enumerable.Repeat(0,
                this.Configuration.OperationDelayBound).ToList();
        }

        #endregion
    }
}
