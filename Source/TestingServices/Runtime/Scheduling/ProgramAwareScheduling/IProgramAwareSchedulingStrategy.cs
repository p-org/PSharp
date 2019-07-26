// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PSharp.Runtime;

namespace Microsoft.PSharp.TestingServices.Scheduling.Strategies
{
    /// <summary>
    /// An interface for schedulers which are aware of the overall structure of the program.
    /// </summary>
    public interface IProgramAwareSchedulingStrategy : ISchedulingStrategy
    {
        /// <summary>
        /// Records a machine executing a Create operation
        /// </summary>
        /// <param name="createdMachine">The created machine</param>
        /// <param name="creatorMachine">The creator machine</param>
        void RecordCreateMachine(Machine createdMachine, Machine creatorMachine);

        /// <summary>
        /// Records a monitor event
        /// </summary>
        /// <param name="monitorType">The type of monitor invoked</param>
        /// <param name="sender">The machine which invoked the monitor</param>
        /// <param name="e">The event used to invoke</param>
        void RecordMonitorEvent(Type monitorType, AsyncMachine sender, Event e);

        /// <summary>
        /// Records the start of a machine.
        /// </summary>
        /// <param name="machine">The starting machine</param>
        /// <param name="initialEvent">The event it was initialized with</param>
        void RecordStartMachine(Machine machine, Event initialEvent);

        /// <summary>
        /// Records a executing a Send operation
        /// </summary>
        /// <param name="sender">The sender machine</param>
        /// <param name="targetMachineId">The recipient machine</param>
        /// <param name="e">The event being sent</param>
        void RecordSendEvent(AsyncMachine sender, MachineId targetMachineId, Event e);

        /// <summary>
        /// Records a executing a Send operation
        /// </summary>
        /// <param name="machine">The machine receiving the event</param>
        /// <param name="e">The event</param>
        void RecordReceiveEvent(Machine machine, Event e);

        // Non-det choices

        /// <summary>
        /// Records a non-deterministic boolean choice occuring in the program
        /// </summary>
        /// <param name="boolChoice">The choice</param>
        void RecordNonDetBooleanChoice(bool boolChoice);

        /// <summary>
        /// Records a non-deterministic integer choice occuring in the program
        /// </summary>
        /// <param name="intChoice">The choice</param>
        void RecordNonDetIntegerChoice(int intChoice);

        // Functions to keep things neat.

        /// <summary>
        /// To be called when the scheduler has stopped scheduling
        /// ( whether it completed, hit the bound or found a bug )
        /// </summary>
        void NotifySchedulingEnded(bool bugFound);

        /// <summary>
        /// Return a report of the schedule - use for statistics etc.s
        /// </summary>
        /// <returns>An informative string</returns>
        string GetReport();
    }
}
