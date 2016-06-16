//-----------------------------------------------------------------------
// <copyright file="ThreadMonitorProcess.cs">
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

namespace Microsoft.PSharp.Monitoring
{
    /// <summary>
    /// A P# thread monitoring process.
    /// </summary>
    internal sealed class ThreadMonitorProcess
    {
        #region fields

        /// <summary>
        /// Configuration.
        /// </summary>
        private Configuration Configuration;

        #endregion

        #region API

        /// <summary>
        /// Creates a P# thread monitoring process.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <returns>ThreadMonitorProcess</returns>
        public static ThreadMonitorProcess Create(Configuration configuration)
        {
            return new ThreadMonitorProcess(configuration);
        }

        /// <summary>
        /// Starts the P# testing process.
        /// </summary>
        public void Start()
        {
            IO.PrintLine(". Testing " + this.Configuration.AssemblyToBeAnalyzed);
            this.MonitorAssembly(this.Configuration.AssemblyToBeAnalyzed);
        }

        #endregion

        #region private methods

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        private ThreadMonitorProcess(Configuration configuration)
        {
            this.Configuration = configuration;
        }
        
        /// <summary>
        /// Monitors the given P# assembly.
        /// </summary>
        /// <param name="dll">Assembly</param>
        private void MonitorAssembly(string dll)
        {
            // Creates and runs the P# testing engine to find bugs in the P# program.
            //TestingEngineFactory.CreateBugFindingEngine(this.Configuration, dll).Run();

            System.Console.WriteLine("reached " + dll);

            //var path = Assembly.GetAssembly(typeof(Program)).Location;
            //var assembly = Assembly.LoadFrom(executable);

            //string[] searchDirectories = new[]{
            //    Path.GetDirectoryName(path),
            //    Path.GetDirectoryName(assembly.Location),
            //};

            //var resolver = new AssemblyResolver();
            //Array.ForEach(searchDirectories, d => resolver.AddSearchDirectory(d));
            //resolver.Attach();
            //var engine = new RemoteRaceInstrumentationEngine();
            //engine.Execute(executable, args);
        }
        
        #endregion
    }
}
