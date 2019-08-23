// ------------------------------------------------------------------------------------------------

using Microsoft.ExtendedReflection.Collections;
using Microsoft.PSharp.IO;
using Microsoft.PSharp.TestingServices;
using System;
using System.IO;
using System.Reflection;

namespace Microsoft.PSharp.Monitoring
{
    /// <summary>
    /// The remote race instrumentation engine.
    /// </summary>
    [Serializable]
    internal class RemoteRaceInstrumentationEngine : MarshalByRefObject
    {
        #region fields

        /// <summary>
        /// Map of assemblies.
        /// </summary>
        private static SafeDictionary<string, Assembly> Assemblies;

        #endregion fields

        #region constructors

        /// <summary>
        /// Static constructor.
        /// </summary>
        static RemoteRaceInstrumentationEngine()
        {
            Assemblies = new SafeDictionary<string, Assembly>();
        }

        #endregion constructors

        #region public methods

        /// <summary>
        /// Invokes the P# testing engine.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        public void Execute(Configuration configuration)
        {
            // Creates and runs the P# testing engine to find bugs in the P# program.
            ITestingEngine testingEngine = TestingEngineFactory.CreateBugFindingEngine(configuration);

            var assembly = Assembly.LoadFrom(configuration.AssemblyToBeAnalyzed);
            new RaceInstrumentationEngine(testingEngine.Reporter, configuration);

            this.TryLoadReferencedAssemblies(new[] { assembly });

            testingEngine.Run();

            Output.WriteLine(testingEngine.Report());
            if (testingEngine.TestReport.NumOfFoundBugs > 0)
            {
                string file = Path.GetFileNameWithoutExtension(configuration.AssemblyToBeAnalyzed);
                file += "_" + configuration.TestingProcessId;

                string directoryPath;
                string suffix = "";

                if (configuration.OutputFilePath != "")
                {
                    directoryPath = configuration.OutputFilePath + Path.DirectorySeparatorChar;
                }
                else
                {
                    var subpath = Path.GetDirectoryName(configuration.AssemblyToBeAnalyzed);
                    if (subpath == "")
                    {
                        subpath = ".";
                    }

                    directoryPath = subpath +
                        Path.DirectorySeparatorChar + "Output" + Path.DirectorySeparatorChar;
                }

                if (suffix.Length > 0)
                {
                    directoryPath += suffix + Path.DirectorySeparatorChar;
                }

                Directory.CreateDirectory(directoryPath);
                Output.WriteLine($"... Emitting task {configuration.TestingProcessId} traces:");
                testingEngine.TryEmitTraces(directoryPath, file);
            }

            //if (configuration.ReportCodeCoverage)
            //{
            //    testingEngine.TryEmitCoverageReport();
            //}
        }

        public override object InitializeLifetimeService()
        {
            return null;
        }

        #endregion public methods

        #region private methods

        /// <summary>
        /// Tries to load the referenced assemblies.
        /// </summary>
        /// <param name="inputAssemblies">Assemblies</param>
        private void TryLoadReferencedAssemblies(Assembly[] inputAssemblies)
        {
            var ws = new SafeDictionary<string, Assembly>();

            foreach (Assembly a in inputAssemblies)
            {
                if (a == null)
                {
                    continue;
                }

                // recursively load all the assemblies reachables from the root!
                if (!Assemblies.ContainsKey(
                    a.GetName().FullName) && !ws.ContainsKey(a.GetName().FullName))
                {
                    ws.Add(a.GetName().FullName, a);
                }

                while (ws.Count > 0)
                {
                    var en = ws.Keys.GetEnumerator();
                    en.MoveNext();
                    var a_name = en.Current;
                    var a_assembly = ws[a_name];
                    Assemblies.Add(a_name, a_assembly);
                    ws.Remove(a_name);

                    foreach (AssemblyName name in a_assembly.GetReferencedAssemblies())
                    {
                        Assembly b;
                        ExtendedReflection.Utilities.ReflectionHelper.TryLoadAssembly(name.FullName, out b);

                        if (b != null)
                        {
                            if (!Assemblies.ContainsKey(b.GetName().FullName) &&
                                !ws.ContainsKey(b.GetName().FullName))
                            {
                                ws.Add(b.GetName().FullName, b);
                            }
                        }
                    }
                }
            }
        }

        #endregion private methods
    }
}