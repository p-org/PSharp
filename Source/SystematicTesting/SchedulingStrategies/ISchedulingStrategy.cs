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

namespace Microsoft.PSharp.SystematicTesting.Scheduling
{
    /// <summary>
    /// Interface of a generic machine scheduling strategy.
    /// </summary>
    public interface ISchedulingStrategy
    {
        /// <summary>
        /// Returns the next machine to schedule.
        /// </summary>
        /// <param name="next">Next</param>
        /// <param name="machines">Machines</param>
        /// <param name="currentMachine">Curent machine</param>
        /// <returns>Boolean value</returns>
        bool TryGetNext(out MachineInfo next, List<MachineInfo> machines, MachineInfo currentMachine);

        /// <summary>
        /// Returns the next choice.
        /// </summary>
        /// <param name="maxValue">Max value</param>
        /// <param name="next">Next</param>
        /// <returns>Boolean value</returns>
        bool GetNextChoice(int maxValue, out bool next);

        /// <summary>
        /// Returns the explored scheduling steps.
        /// </summary>
        /// <returns>Scheduling steps</returns>
        int GetSchedulingSteps();

        /// <summary>
        /// Returns the maximum explored scheduling steps.
        /// </summary>
        /// <returns>Scheduling steps</returns>
        int GetMaxSchedulingSteps();

        /// <summary>  
        /// Returns the depth bound.
        /// </summary> 
        /// <returns>Depth bound</returns>  
        int GetDepthBound();

        /// <summary>
        /// True if the scheduling strategy reached the depth bound
        /// for the given scheduling iteration.
        /// </summary>
        /// <returns>Depth bound</returns>
        bool HasReachedDepthBound();

        /// <summary>
        /// True if the scheduling has finished.
        /// </summary>
        /// <returns>Boolean value</returns>
        bool HasFinished();

        /// <summary>
        /// Prepares the next scheduling iteration.
        /// </summary>
        void ConfigureNextIteration();

        /// <summary>
        /// Resets the scheduling strategy.
        /// </summary>
        void Reset();

        /// <summary>
        /// Returns a textual description of the scheduling strategy.
        /// </summary>
        /// <returns>String</returns>
        string GetDescription();
    }
}
