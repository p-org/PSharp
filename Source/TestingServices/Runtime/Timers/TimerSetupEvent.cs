﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.PSharp.Timers;

namespace Microsoft.PSharp.TestingServices.Timers
{
    /// <summary>
    /// Defines a timer elapsed event that is sent from a timer to the machine that owns the timer.
    /// </summary>
    internal class TimerSetupEvent : Event
    {
        /// <summary>
        /// Stores information about the timer.
        /// </summary>
        internal readonly TimerInfo Info;

        /// <summary>
        /// The machine that owns the timer.
        /// </summary>
        internal readonly Machine Owner;

        /// <summary>
        /// Adjusts the probability of firing a timeout event.
        /// </summary>
        internal readonly uint Delay;

        /// <summary>
        /// Creates a new instance of the <see cref="TimerElapsedEvent"/> class.
        /// </summary>
        /// <param name="info">Stores information about the timer.</param>
        /// <param name="owner">The machine that owns the timer.</param>
        /// <param name="delay">Adjusts the probability of firing a timeout event.</param>
        internal TimerSetupEvent(TimerInfo info, Machine owner, uint delay)
        {
            this.Info = info;
            this.Owner = owner;
            this.Delay = delay;
        }
    }
}
