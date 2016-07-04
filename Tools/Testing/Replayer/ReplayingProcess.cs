//-----------------------------------------------------------------------
// <copyright file="ReplayingProcess.cs">
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

using Microsoft.PSharp.TestingServices;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp
{
    /// <summary>
    /// A P# replaying process.
    /// </summary>
    internal sealed class ReplayingProcess
    {
        #region fields

        /// <summary>
        /// Configuration.
        /// </summary>
        private Configuration Configuration;

        #endregion

        #region public methods

        /// <summary>
        /// Creates a P# replaying process.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <returns>ReplayingProcess</returns>
        public static ReplayingProcess Create(Configuration configuration)
        {
            return new ReplayingProcess(configuration);
        }

        /// <summary>
        /// Starts the P# replaying process.
        /// </summary>
        public void Start()
        {
            IO.PrintLine(". Reproducing trace in " + this.Configuration.AssemblyToBeAnalyzed);

            // Creates a new P# replay engine to reproduce a bug.
            ITestingEngine engine = TestingEngineFactory.CreateReplayEngine(this.Configuration);

            engine.Run();
            IO.PrintLine(engine.Report());
        }

        #endregion

        #region private methods

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        private ReplayingProcess(Configuration configuration)
        {
            configuration.SchedulingStrategy = SchedulingStrategy.Replay;
            this.Configuration = configuration;
        }

        #endregion
    }
}
