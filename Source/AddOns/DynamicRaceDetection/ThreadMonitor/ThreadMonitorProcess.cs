// ------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Reflection;

using Microsoft.ExtendedReflection.Utilities;

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
            // configuration.EnableDataRaceDetection = true;
            //configuration.RaceDetectionCallback = new Action(() => {
            //    bool result = new FinalRaceDetector.RaceDetectionEngine(configuration).Start();
            //    if (result == true)
            //    {
            //        configuration.RaceFound = true;
            //    }
            //});
        }

        /// <summary>
        /// Monitors the specified P# assembly.
        /// </summary>
        /// <param name="dll">Assembly</param>
        private void MonitorAssembly(string dll)
        {
            var path = Assembly.GetAssembly(typeof(Program)).Location;
            var assembly = Assembly.LoadFrom(dll);

            string[] searchDirectories = new[]{
                Path.GetDirectoryName(path),
                Path.GetDirectoryName(assembly.Location),
            };

            var resolver = new AssemblyResolver();
            Array.ForEach(searchDirectories, d => resolver.AddSearchDirectory(d));
            resolver.Attach();

            var engine = new RemoteRaceInstrumentationEngine();
            engine.Execute(this.Configuration);
        }
        
        #endregion
    }
}
