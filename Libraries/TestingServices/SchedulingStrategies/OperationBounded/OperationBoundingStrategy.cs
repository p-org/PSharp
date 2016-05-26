//-----------------------------------------------------------------------
// <copyright file="OperationBoundingStrategy.cs">
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
    /// Class representing an abstract operation-bounding scheduling strategy.
    /// </summary>
    public abstract class OperationBoundingStrategy : ISchedulingStrategy
    {
        #region fields

        /// <summary>
        /// The configuration.
        /// </summary>
        protected Configuration Configuration;

        /// <summary>
        /// List of operations.
        /// </summary>
        protected List<int> Operations;

        /// <summary>
        /// Nondeterminitic seed.
        /// </summary>
        protected int Seed;

        /// <summary>
        /// Randomizer.
        /// </summary>
        protected Random Random;

        /// <summary>
        /// The maximum number of explored steps.
        /// </summary>
        protected int MaxExploredSteps;

        /// <summary>
        /// The number of explored steps.
        /// </summary>
        protected int ExploredSteps;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        public OperationBoundingStrategy(Configuration configuration)
        {
            this.Configuration = configuration;
            this.Operations = new List<int>();
            this.Seed = this.Configuration.RandomSchedulingSeed ?? DateTime.Now.Millisecond;
            this.Random = new Random(this.Seed);
            this.MaxExploredSteps = 0;
            this.ExploredSteps = 0;
        }

        /// <summary>
        /// Returns the next machine to schedule.
        /// </summary>
        /// <param name="next">Next</param>
        /// <param name="choices">Choices</param>
        /// <param name="current">Curent</param>
        /// <returns>Boolean</returns>
        public bool TryGetNext(out MachineInfo next, IList<MachineInfo> choices, MachineInfo current)
        {
            if (this.HasCurrentOperationCompleted(choices, current))
            {
                this.Operations.Remove(current.Machine.OperationId);
                IO.Debug("<OperationDebug> Removes operation '{0}'.", current.Machine.OperationId);
            }

            var availableMachines = choices.Where(
                mi => mi.IsEnabled && !mi.IsBlocked && !mi.IsWaitingToReceive).ToList();
            if (availableMachines.Count == 0)
            {
                availableMachines = choices.Where(m => m.IsWaitingToReceive).ToList();
                if (availableMachines.Count == 0)
                {
                    next = null;
                    return false;
                }
            }

            this.TryRegisterNewOperations(availableMachines, current);
            
            var nextOperation = this.GetNextOperation(availableMachines, current);

            IO.Debug("<OperationDebug> Chosen operation '{0}'.", nextOperation);
            if (IO.Debugging)
            {
                IO.Print("<OperationDebug> Operation list: ");
                for (int opIdx = 0; opIdx < this.Operations.Count; opIdx++)
                {
                    if (opIdx < this.Operations.Count - 1)
                    {
                        IO.Print("'{0}', ", this.Operations[opIdx]);
                    }
                    else
                    {
                        IO.Print("'{0}'.\n", this.Operations[opIdx]);
                    }
                }
            }
            
            if (this.Configuration.DynamicEventQueuePrioritization)
            {
                var machineChoices = availableMachines.Where(mi => mi.Machine is Machine).
                    Select(m => m.Machine as Machine);
                foreach (var choice in machineChoices)
                {
                    choice.SetQueueOperationPriority(nextOperation);
                }
            }

            next = this.GetNextMachineWithOperation(availableMachines, nextOperation);

            this.ExploredSteps++;

            return true;
        }

        /// <summary>
        /// Returns the next choice.
        /// </summary>
        /// <param name="maxValue">Max value</param>
        /// <param name="next">Next</param>
        /// <returns>Boolean</returns>
        public abstract bool GetNextChoice(int maxValue, out bool next);

        /// <summary>
        /// Returns the explored steps.
        /// </summary>
        /// <returns>Explored steps</returns>
        public int GetExploredSteps()
        {
            return this.ExploredSteps;
        }

        /// <summary>
        /// Returns the maximum explored steps.
        /// </summary>
        /// <returns>Explored steps</returns>
        public int GetMaxExploredSteps()
        {
            return this.MaxExploredSteps;
        }

        /// <summary>  
        /// Returns the depth bound.
        /// </summary> 
        /// <returns>Depth bound</returns>  
        public int GetDepthBound()
        {
            return this.Configuration.DepthBound;
        }

        /// <summary>
        /// True if the scheduling strategy has reached the depth
        /// bound for the given scheduling iteration.
        /// </summary>
        /// <returns>Depth bound</returns>
        public bool HasReachedDepthBound()
        {
            if (this.Configuration.DepthBound == 0)
            {
                return false;
            }

            return this.ExploredSteps == this.GetDepthBound();
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
        /// Configures the next scheduling iteration.
        /// </summary>
        public virtual void ConfigureNextIteration()
        {
            this.MaxExploredSteps = Math.Max(this.MaxExploredSteps, this.ExploredSteps);
            this.ExploredSteps = 0;
            this.Operations.Clear();
        }

        /// <summary>
        /// Resets the scheduling strategy.
        /// </summary>
        public virtual void Reset()
        {
            this.Operations.Clear();
            this.Random = new Random(this.Seed);
            this.MaxExploredSteps = 0;
            this.ExploredSteps = 0;
        }

        /// <summary>
        /// Returns a textual description of the scheduling strategy.
        /// </summary>
        /// <returns>String</returns>
        public abstract string GetDescription();

        #endregion

        #region protected methods

        /// <summary>
        /// Returns the next operation to schedule.
        /// </summary>
        /// <param name="choices">Choices</param>
        /// <param name="current">Curent</param>
        /// <returns>OperationId</returns>
        protected abstract int GetNextOperation(List<MachineInfo> choices, MachineInfo current);

        /// <summary>
        /// Returns the next machine to schedule that has the given operation.
        /// </summary>
        /// <param name="choices">Choices</param>
        /// <param name="operationId">OperationId</param>
        /// <returns>MachineInfo</returns>
        protected virtual MachineInfo GetNextMachineWithOperation(List<MachineInfo> choices, int operationId)
        {
            var availableMachines = choices.Where(
                mi => mi.Machine.OperationId == operationId).ToList();
            int idx = this.Random.Next(availableMachines.Count);
            return availableMachines[idx];
        }

        #endregion

        #region private methods

        /// <summary>
        /// Tries to register any new operations.
        /// </summary>
        /// <param name="choices">Choices</param>
        /// <param name="current">Curent</param>
        private void TryRegisterNewOperations(IEnumerable<MachineInfo> choices, MachineInfo current)
        {
            if (this.Operations.Count == 0)
            {
                this.Operations.Add(current.Machine.OperationId);
            }

            var operationIds = choices.Select(mi => mi.Machine.OperationId).Distinct();
            foreach (var id in operationIds.Where(id => !this.Operations.Contains(id)))
            {
                var opIndex = this.Random.Next(this.Operations.Count) + 1;
                this.Operations.Insert(opIndex, id);
                IO.Debug("<OperationDebug> Detected new operation '{0}' at index '{1}'.", id, opIndex);
            }
        }

        /// <summary>
        /// Returns true if the current operation has completed.
        /// </summary>
        /// <param name="choices">List of machine infos</param>
        /// <param name="current">MachineInfo</param>
        /// <returns>Boolean</returns>
        private bool HasCurrentOperationCompleted(IEnumerable<MachineInfo> choices, MachineInfo current)
        {
            foreach (var choice in choices.Where(mi => !mi.IsCompleted))
            {
                if (choice.Machine.OperationId == current.Machine.OperationId ||
                    choice.Machine.IsOperationPending(current.Machine.OperationId))
                {
                    return false;
                }
            }

            return true;
        }

        #endregion
    }
}
