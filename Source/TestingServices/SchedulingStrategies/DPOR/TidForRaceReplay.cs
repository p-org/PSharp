using System.Collections.Generic;

namespace Microsoft.PSharp.TestingServices.Scheduling
{
    /// <summary>
    /// Stores a thread id for replaying a race.
    /// Also stores the nondeterministic choices made by a thread.
    /// </summary>
    public class TidForRaceReplay
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