// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.PSharp.TestingServices.SchedulingStrategies.DPOR
{
    /// <summary>
    /// Stores a thread id for replaying a race.
    /// Also stores the nondeterministic choices made by a thread.
    /// </summary>
    internal class TidForRaceReplay
    {
        /// <summary>
        /// The thread id for replay.
        /// </summary>
        public readonly int Id;

        /// <summary>
        /// The list of nondet choices for replay.
        /// </summary>
        public readonly List<NonDetChoice> NondetChoices;

        /// <summary>
        /// Construct a thread id for replay.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="nondetChoices"></param>
        public TidForRaceReplay(int id, List<NonDetChoice> nondetChoices)
        {
            Id = id;
            NondetChoices = nondetChoices;
        }
    }
}