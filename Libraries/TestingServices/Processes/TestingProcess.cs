//-----------------------------------------------------------------------
// <copyright file="TestingProcess.cs">
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

using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.TestingServices
{
    /// <summary>
    /// A P# testing process.
    /// </summary>
    public class TestingProcess : MarshalByRefObject
    {
        #region fields

        /// <summary>
        /// The unique id of the testing process.
        /// </summary>
        public int Id { get; private set; }

        /// <summary>
        /// Configuration.
        /// </summary>
        private Configuration Configuration;

        /// <summary>
        /// The testing engine associated with
        /// this testing process.
        /// </summary>
        private ITestingEngine TestingEngine;

        /// <summary>
        /// An event to be fired when a bug is detected.
        /// </summary>
        public event NotificationHandler HandleDetectedBug;

        #endregion

        #region public methods

        /// <summary>
        /// Configures the testing process.
        /// </summary>
        /// <param name="id">Unique process id</param>
        /// <param name="configuration">Configuration</param>
        public void Configure(int id, Configuration configuration)
        {
            this.Id = id;
            this.Configuration = configuration;
        }

        /// <summary>
        /// Starts the testing process.
        /// </summary>
        public void Start()
        {
            this.TestingEngine = TestingEngineFactory.CreateBugFindingEngine(
                this.Configuration);
            this.TestingEngine.Run();
            if (this.TestingEngine.NumOfFoundBugs > 0)
            {
                this.HandleDetectedBug(this.Id);
            }
        }

        /// <summary>
        /// Stops the testing process.
        /// </summary>
        public void Stop()
        {
            this.TestingEngine.Stop();
        }

        /// <summary>
        /// Tries to emit the traces, if any.
        /// </summary>
        public void TryEmitTraces()
        {
            this.TestingEngine.TryEmitTraces();
        }

        /// <summary>
        /// Reports the testing results.
        /// </summary>
        public void Report()
        {
            this.TestingEngine.Report();
        }

        /// <summary>
        /// A testing process notification event handler.
        /// </summary>
        /// <param name="processId">Unique process id</param>
        public delegate void NotificationHandler(int processId);

        #endregion
    }
}
