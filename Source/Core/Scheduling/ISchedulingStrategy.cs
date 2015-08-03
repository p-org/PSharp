//-----------------------------------------------------------------------
// <copyright file="ISchedulingStrategy.cs" company="Microsoft">
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

namespace Microsoft.PSharp.Scheduling
{
    /// <summary>
    /// Interface of a generic scheduling strategy.
    /// </summary>
    public interface ISchedulingStrategy
    {
        /// <summary>
        /// Returns the next task to schedule.
        /// </summary>
        /// <param name="next">Next</param>
        /// <param name="tasks">Tasks</param>
        /// <returns>Boolean value</returns>
        bool TryGetNext(out TaskInfo next, List<TaskInfo> tasks);

        /// <summary>
        /// Returns the next choice.
        /// </summary>
        /// <param name="next">Next</param>
        /// <returns>Boolean value</returns>
        bool GetNextChoice(out bool next);

        /// <summary>
        /// Returns the depth bound.
        /// </summary>
        /// <returns>Depth bound</returns>
        int GetDepthBound();

        /// <summary>
        /// True if the scheduling has finished.
        /// </summary>
        /// <returns>Boolean value</returns>
        bool HasFinished();

        /// <summary>
        /// Returns a textual description of the scheduling strategy.
        /// </summary>
        /// <returns>String</returns>
        string GetDescription();

        /// <summary>
        /// Advances the scheduling strategy.
        /// </summary>
        void Advance();

        /// <summary>
        /// Resets the scheduling strategy.
        /// </summary>
        void Reset();
    }
}
