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
        /// Called when the ControlUnitStrategy is being intiialized
        /// </summary>
        /// <param name="configuration">The configuration passed to the ControlUnitStrategy</param>
        /// <param name="nextStrategy">The strategy to be used</param>
        void Initialize(Configuration configuration, out ISchedulingStrategy nextStrategy);

        /// <summary>
        /// Called by ControlUnitStrategy.
        /// </summary>
        void StrategyReset();

        /// <summary>
        /// Called by ControlUnitStrategy.
        /// </summary>
        /// <param name="nextStrategy">The ControlUnitStrategy that is being used</param>
        /// <param name="configurationForNextIter">Changes such as Max[Un]FairSchedulingSteps / LivenessTreshold can be modified for next iterations</param>
        /// <returns>true/false according to what ControlUnitStrategy must return</returns>
        bool StrategyPrepareForNextIteration(out ISchedulingStrategy nextStrategy, Configuration configurationForNextIter);

        /// <summary>
        /// Called when the iteration is done.
        /// </summary>
        /// <param name="bugFound">Was the bug found in this iteration</param>
        void NotifySchedulingEnded(bool bugFound);

        /// <summary>
        /// Tell the runtime whether or not to enqueue this event
        /// </summary>
        /// <param name="senderId">MachineId of the sender</param>
        /// <param name="targetId">MachineId of the target</param>
        /// <param name="evt">The event being sent</param>
        /// <returns>true if the event must be enqueued</returns>
        bool ShouldEnqueueEvent(MachineId senderId, MachineId targetId, Event evt);

        /// <summary>
        /// A report which will be printed at the end.
        /// </summary>
        /// <returns>The report</returns>
        string GetReport();
    }
}
