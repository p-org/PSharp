//-----------------------------------------------------------------------
// <copyright file="RandomOperationBoundingStrategy.cs" company="Microsoft">
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

namespace Microsoft.PSharp.SystematicTesting.Scheduling
{
    /// <summary>
    /// Class representing a random operation-bounding scheduling strategy.
    /// </summary>
    public class RandomOperationBoundingStrategy : OperationBoundingStrategy, ISchedulingStrategy
    {
        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        public RandomOperationBoundingStrategy(Configuration configuration)
            : base(configuration)
        {
            
        }

        /// <summary>
        /// Returns the next choice.
        /// </summary>
        /// <param name="maxValue">Max value</param>
        /// <param name="next">Next</param>
        /// <returns>Boolean</returns>
        public override bool GetNextChoice(int maxValue, out bool next)
        {
            next = false;

            if (base.Random.Next(maxValue) == 0)
            {
                next = true;
            }

            this.ExploredSteps++;

            return true;
        }

        /// <summary>
        /// Configures the next scheduling iteration.
        /// </summary>
        public override void ConfigureNextIteration()
        {
            base.ConfigureNextIteration();
        }

        /// <summary>
        /// Resets the scheduling strategy.
        /// </summary>
        public override void Reset()
        {
            base.Reset();
        }

        /// <summary>
        /// Returns a textual description of the scheduling strategy.
        /// </summary>
        /// <returns>String</returns>
        public override string GetDescription()
        {
            var text = "Random seed '" + base.Seed + "'.";
            return text;
        }

        #endregion

        #region protected methods

        /// <summary>
        /// Returns the next operation to schedule.
        /// </summary>
        /// <param name="choices">Choices</param>
        /// <param name="current">Curent</param>
        /// <returns>OperationId</returns>
        protected override int GetNextOperation(List<MachineInfo> choices, MachineInfo current)
        {
            var enabledOperations = base.Operations.Where(val => choices.Any(
                m => m.Machine.OperationId == val)).ToList();
            int opIdx = base.Random.Next(enabledOperations.Count);
            return enabledOperations[opIdx];
        }

        #endregion

        #region private methods

        /// <summary>
        /// Returns a random enabled operation id.
        /// </summary>
        /// <param name="choices">Choices</param>
        /// <returns>OperationId</returns>
        private int GetRandomEnabledOperationId(IEnumerable<MachineInfo> choices)
        {
            var enabledOperations = base.Operations.Where(val => choices.Any(
                m => m.Machine.OperationId == val)).ToList();
            int opIdx = base.Random.Next(enabledOperations.Count);
            var prioritizedOperation = enabledOperations[opIdx];
            return prioritizedOperation;
        }

        #endregion
    }
}
