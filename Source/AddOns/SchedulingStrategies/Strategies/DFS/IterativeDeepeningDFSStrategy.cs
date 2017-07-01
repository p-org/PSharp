//-----------------------------------------------------------------------
// <copyright file="IterativeDeepeningDFSStrategy.cs">
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

namespace Microsoft.TestingServices.SchedulingStrategies
{
    /// <summary>
    /// A depth-first search scheduling strategy that uses iterative deepening.
    /// </summary>
    public sealed class IterativeDeepeningDFSStrategy : DFSStrategy, ISchedulingStrategy
    {
        /// <summary>
        /// The max depth.
        /// </summary>
        private int MaxDepth;

        /// <summary>
        /// The current depth.
        /// </summary>
        private int CurrentDepth;

        /// <summary>
        /// Creates a DFS strategy that uses iterative deepening.
        /// </summary>
        /// <param name="maxSteps">Max scheduling steps</param>
        /// <param name="logger">ILogger</param>
        public IterativeDeepeningDFSStrategy(int maxSteps, ILogger logger)
            : base(maxSteps, logger)
        {
            MaxDepth = maxSteps;
            CurrentDepth = 1;
        }

        /// <summary>
        /// Prepares for the next scheduling iteration. This is invoked
        /// at the end of a scheduling iteration. It must return false
        /// if the scheduling strategy should stop exploring.
        /// </summary>
        /// <returns>True to start the next iteration</returns>
        public override bool PrepareForNextIteration()
        {
            bool doNext = PrepareForNextIteration();
            if (!doNext)
            {
                Reset();
                CurrentDepth++;
                if (CurrentDepth <= MaxDepth)
                {
                    Logger.WriteLine($"<IterativeDeepeningDFSLog> Depth bound increased to {CurrentDepth} (max is {MaxDepth}).");
                    doNext = true;
                }
            }

            return doNext;
        }

        /// <summary>
        /// True if the scheduling strategy has reached the max
        /// scheduling steps for the given scheduling iteration.
        /// </summary>
        /// <returns>Depth bound</returns>
        public new bool HasReachedMaxSchedulingSteps()
        {
            return ScheduledSteps == CurrentDepth;
        }

        /// <summary>
        /// Returns a textual description of the scheduling strategy.
        /// </summary>
        /// <returns>String</returns>
        public new string GetDescription()
        {
            return $"DFS with iterative deepening (max depth is {MaxDepth})";
        }
    }
}
