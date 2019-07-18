// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.PSharp.TestingServices.Scheduling.Strategies;

namespace Microsoft.PSharp.TestingServices.Scheduling.ClientInterface
{
    /// <summary>
    /// A controller to help drive the ControlUnitStrategy. Should allow clients to do funky stuff.
    /// </summary>
    public interface IStrategyController
    {
        /// <summary>
        /// Called by ControlUnitStrategy.
        /// </summary>
        /// <param name="nextStrategy">The ControlUnitStrategy that is being used</param>
        /// <param name="configurationForNextIter">Changes such as Max[Un]FairSchedulingSteps / LivenessTreshold can be modified for next iterations</param>
        /// <returns>true/false according to what ControlUnitStrategy must return</returns>
        bool StrategyPrepareForNextIteration(out ISchedulingStrategy nextStrategy, Configuration configurationForNextIter);

        /// <summary>
        /// Called by ControlUnitStrategy.
        /// </summary>
        void StrategyReset();

        /// <summary>
        /// A report which will be printed at the end.
        /// </summary>
        /// <returns>The report</returns>
        string GetReport();

        /// <summary>
        /// Called when the iteration is done.
        /// </summary>
        /// <param name="bugFound">Was the bug found in this iteration</param>
        void NotifySchedulingEnded(bool bugFound);

        // There is no need for this.
        // /// <summary>
        // ///  Called once by ControlUnitStrategy to register itself with the Controller
        // /// </summary>
        // /// <param name="controlUnitStrategy">The ControlUnitStrategy</param>
        // void SetControlUnitStrategy(ControlUnitStrategy controlUnitStrategy);
    }
}
