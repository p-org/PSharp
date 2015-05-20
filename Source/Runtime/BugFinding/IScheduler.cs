//-----------------------------------------------------------------------
// <copyright file="IScheduler.cs" company="Microsoft">
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

using System.Collections.Generic;

namespace Microsoft.PSharp.BugFinding
{
    /// <summary>
    /// Interface of a generic scheduler.
    /// </summary>
    public interface IScheduler
    {
        /// <summary>
        /// Returns the next machine to schedule.
        /// </summary>
        /// <param name="machines">Machines</param>
        /// <returns>Machine</returns>
        Machine Next(List<Machine> machines);

        /// <summary>
        /// Returns number of scheduling points.
        /// </summary>
        /// <returns>Integer value</returns>
        int GetNumOfSchedulingPoints();

        /// <summary>
        /// Returns a textual description of the scheduler.
        /// </summary>
        /// <returns>String</returns>
        string GetDescription();

        /// <summary>
        /// Resets the scheduling strategy.
        /// </summary>
        /// <returns>Boolean value</returns>
        bool Reset();
    }
}
