//-----------------------------------------------------------------------
// <copyright file="TidForRaceReplay.cs">
//      Copyright (c) Microsoft Corporation. All rights reserved.
// 
//      THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//      EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//      MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//      IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//      CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//      TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//      SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
//-----------------------------------------------------------------------

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