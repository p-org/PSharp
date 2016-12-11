//-----------------------------------------------------------------------
// <copyright file="RunToCompletionStrategy.cs">
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

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.TestingServices.Scheduling
{
    /// <summary>
    /// Class representing an abstract delay-bounding scheduling strategy.
    /// </summary>
    public class RunToCompletionStrategy : ISchedulingStrategy
    {
        #region fields

        /// <summary>
        /// The configuration.
        /// </summary>
        protected Configuration Configuration;

        /// <summary>
        /// Nondeterminitic seed.
        /// </summary>
        protected int Seed;

        /// <summary>
        /// Randomizer.
        /// </summary>
        protected IRandomNumberGenerator Random;

        /// <summary>
        /// The number of explored steps.
        /// </summary>
        protected int ExploredSteps;

        /// <summary>
        /// The maximum number of delays.
        /// </summary>
        protected int MaxDelays;

        /// <summary>
        /// Delays used in the current iteration
        /// </summary>
        protected List<int> Delays;

        /// <summary>
        /// The remaining delays.
        /// </summary>
        protected List<int> RemainingDelays;

        /// <summary>
        /// Queue used for ordering machines
        /// </summary>
        protected List<int> MachineQueue;

        /// <summary>
        /// Record of Boolean choices made
        /// </summary>
        protected List<bool> BooleanChoices;

        /// <summary>
        /// Current index into BooleanChoices
        /// </summary>
        protected int BooleanChoicesIndex;

        /// <summary>
        /// Record of Integer choices made
        /// </summary>
        protected List<int> IntegerChoices;

        /// <summary>
        /// Current index into BooleanChoices
        /// </summary>
        protected int IntegerChoicesIndex;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="delays">Max number of delays</param>
        public RunToCompletionStrategy(Configuration configuration, int delays)
        {
            this.Configuration = configuration;
            this.Seed = this.Configuration.RandomSchedulingSeed ?? DateTime.Now.Millisecond;
            this.Random = new RandomWrapper(this.Seed);
            this.ExploredSteps = 0;
            this.MaxDelays = delays;
            this.Delays = new List<int>();
            this.RemainingDelays = new List<int>();
            this.BooleanChoices = new List<bool>();
            this.IntegerChoices = new List<int>();
            this.BooleanChoicesIndex = 0;
            this.IntegerChoicesIndex = 0;
            this.MachineQueue = new List<int>();
        }

        /// <summary>
        /// Returns the next machine to schedule.
        /// </summary>
        /// <param name="next">Next</param>
        /// <param name="choices">Choices</param>
        /// <param name="current">Curent</param>
        /// <returns>Boolean</returns>
        public virtual bool TryGetNext(out MachineInfo next, IEnumerable<MachineInfo> choices, MachineInfo current)
        {
            var idToMachine = new Dictionary<int, MachineInfo>();

            foreach(var mi in choices.Where(mi => mi.IsEnabled && !mi.IsBlocked && !mi.IsWaitingToReceive))
            {
                idToMachine.Add(mi.Machine.Id.Value, mi);
            }

            if(idToMachine.Count == 0)
            {
                next = choices.Where(mi => mi.IsWaitingToReceive).FirstOrDefault();
                return next != null;
            }

            // Check Shaz's invariant: enabled \subseteq queue
            var enabledMachines = new HashSet<int>(idToMachine.Keys);
            var queueContents = new HashSet<int>(this.MachineQueue);
            if(!enabledMachines.IsSubsetOf(queueContents))
            {
                throw new PSharpException("Invariant failure inside RunToCompletionStrategy");
            }

            // Delay
            while (this.RemainingDelays.Count > 0 && this.ExploredSteps == this.RemainingDelays[0])
            {
                // Move from front to back
                var frontElement = MachineQueue[0];
                this.MachineQueue.Add(frontElement);
                this.MachineQueue.RemoveAt(0);

                // spend the delay
                this.RemainingDelays.RemoveAt(0);
                IO.PrintLine("<DelayLog> Inserted delay, '{0}' remaining.", this.RemainingDelays.Count);

                if(this.RemainingDelays.Count == 0)
                {
                    this.BooleanChoices.RemoveRange(this.BooleanChoicesIndex, this.BooleanChoices.Count - this.BooleanChoicesIndex);
                    this.IntegerChoices.RemoveRange(this.IntegerChoicesIndex, this.IntegerChoices.Count - this.IntegerChoicesIndex);
                }
            }

            while (this.MachineQueue.Count > 0 && !idToMachine.ContainsKey(MachineQueue[0]))
            {
                // remove top element
                this.MachineQueue.RemoveAt(0);
            }

            if(this.MachineQueue.Count == 0)
            {
                throw new PSharpException("Invariant failure inside RunToCompletionStrategy");
            }

            next = idToMachine[this.MachineQueue[0]];

            this.ExploredSteps++;

            return true;
        }

        /// <summary>
        /// Returns the next boolean choice.
        /// </summary>
        /// <param name="maxValue">Max value</param>
        /// <param name="next">Next</param>
        /// <returns>Boolean</returns>
        public virtual bool GetNextBooleanChoice(int maxValue, out bool next)
        {
            if (this.RemainingDelays.Count == 0)
            {
                next = Random.Next() % 2 == 0;
                this.BooleanChoices.Add(next);
            }
            else if (this.BooleanChoicesIndex < this.BooleanChoices.Count)
            {
                next = this.BooleanChoices[this.BooleanChoicesIndex];
                this.BooleanChoicesIndex++;
            }
            else
            {
                next = Random.Next() % 2 == 0;
            }

            return true;
        }

        /// <summary>
        /// Returns the next integer choice.
        /// </summary>
        /// <param name="maxValue">Max value</param>
        /// <param name="next">Next</param>
        /// <returns>Boolean</returns>
        public virtual bool GetNextIntegerChoice(int maxValue, out int next)
        {
            if (this.RemainingDelays.Count == 0)
            {
                next = Random.Next(maxValue);
                this.IntegerChoices.Add(next);
            }
            else if (this.IntegerChoicesIndex < this.IntegerChoices.Count)
            {
                next = this.IntegerChoices[this.IntegerChoicesIndex] % maxValue;
                this.IntegerChoicesIndex++;
            }
            else
            {
                next = Random.Next(maxValue);
            }

            return true;
        }

        /// <summary>
        /// Returns the explored steps.
        /// </summary>
        /// <returns>Explored steps</returns>
        public int GetExploredSteps()
        {
            return this.ExploredSteps;
        }

        /// <summary>
        /// True if the scheduling strategy has reached the max
        /// scheduling steps for the given scheduling iteration.
        /// </summary>
        /// <returns>Boolean</returns>
        public bool HasReachedMaxSchedulingSteps()
        {
            var bound = (this.IsFair() ? this.Configuration.MaxFairSchedulingSteps :
                this.Configuration.MaxUnfairSchedulingSteps);

            if (bound == 0)
            {
                return false;
            }

            return this.ExploredSteps >= bound;
        }

        /// <summary>
        /// Returns true if the scheduling has finished.
        /// </summary>
        /// <returns>Boolean</returns>
        public bool HasFinished()
        {
            return false;
        }

        /// <summary>
        /// Checks if this a fair scheduling strategy.
        /// </summary>
        /// <returns>Boolean</returns>
        public bool IsFair()
        {
            return false;
        }

        /// <summary>
        /// Configures the next scheduling iteration.
        /// </summary>
        public void ConfigureNextIteration()
        {
            this.MachineQueue = new List<int>();

            var remainingSteps = this.ExploredSteps -
                (this.Delays.Count > 0 ? this.Delays.Last() : 0);

            if (this.Delays.Count == this.MaxDelays || remainingSteps <= 0)
            {
                if(remainingSteps < 0)
                {
                    IO.PrintLine("<DelayLog> Failed to repro last execution.");
                }
                this.Delays = new List<int>();
                this.BooleanChoices.Clear();
                this.IntegerChoices.Clear();
                this.RemainingDelays.Clear();
            }
            else
            {
                var nextDelay = this.Random.Next(remainingSteps);
                this.Delays.Add(nextDelay);
                this.RemainingDelays = new List<int>(this.Delays);
            }
            this.ExploredSteps = 0;
        }

        /// <summary>
        /// Resets the scheduling strategy.
        /// </summary>
        public virtual void Reset()
        {
            this.Random = new RandomWrapper(this.Seed);
            this.ExploredSteps = 0;
            this.Delays.Clear();
            this.RemainingDelays.Clear();
            this.BooleanChoices.Clear();
            this.IntegerChoices.Clear();
            this.MachineQueue = new List<int>();
        }

        /// <summary>
        /// Returns a textual description of the scheduling strategy.
        /// </summary>
        /// <returns>String</returns>
        public string GetDescription()
        {
            return string.Format("RunToCompletion[{0}]", this.MaxDelays);
        }

        /// <summary>
        /// Informs the scheduler of a send operation
        /// </summary>
        /// <param name="source">Source machine (if any)</param>
        /// <param name="payload">Event sent</param>
        /// <param name="destination">Target machine</param>
        public virtual void OnSend(MachineInfo source, Event payload, MachineInfo destination)
        {
            if(this.MachineQueue.Contains(destination.Machine.Id.Value))
            {
                return;
            }
            this.MachineQueue.Insert(0, destination.Machine.Id.Value);
        }


        /// <summary>
        /// Informs the scheduler of a CreateMachine operation
        /// </summary>
        /// <param name="created">The machine created</param>
        public virtual void OnCreateMachine(MachineInfo created)
        {
            this.MachineQueue.Insert(0, created.Machine.Id.Value);
        }

        #endregion
    }
}
