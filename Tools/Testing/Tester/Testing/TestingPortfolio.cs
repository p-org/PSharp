//-----------------------------------------------------------------------
// <copyright file="TestingPortfolio.cs">
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

using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.TestingServices
{
    /// <summary>
    /// The P# testing portfolio.
    /// </summary>
    internal static class TestingPortfolio
    {
        #region internal methods

        /// <summary>
        /// Configures the testing strategy for the current
        /// testing process.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        internal static void ConfigureStrategyForCurrentProcess(Configuration configuration)
        {
            // Random, PCT[1], ProbabilisticRandom[5], PCT[5], ProbabilisticRandom[10], PCT[10], etc.
            if (configuration.TestingProcessId == 0)
            {
                configuration.SchedulingStrategy = SchedulingStrategy.Random;
            }
            else if (configuration.TestingProcessId % 2 == 0)
            {
                configuration.SchedulingStrategy = SchedulingStrategy.ProbabilisticRandom;
                configuration.CoinFlipBound = 5 * (int)(configuration.TestingProcessId / 2);
            }
            else if(configuration.TestingProcessId == 1)
            {
                configuration.SchedulingStrategy = SchedulingStrategy.FairPCT;
                configuration.PrioritySwitchBound = 1; 
            }
            else 
            {
                configuration.SchedulingStrategy = SchedulingStrategy.FairPCT;
                configuration.PrioritySwitchBound = 5 * (int)((configuration.TestingProcessId + 1)/ 2);
            }

        }

        #endregion
    }
}