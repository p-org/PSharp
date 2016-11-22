//-----------------------------------------------------------------------
// <copyright file="RemoteRaceInstrumentationEngine.cs">
//      Copyright (c) 2016 Microsoft Corporation. All rights reserved.
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
using System.Reflection;

using Microsoft.ExtendedReflection.Collections;

using Microsoft.PSharp.TestingServices;
using Microsoft.PSharp.Utilities;

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

        #endregion

        #region constructors

        /// <summary>
        /// Static constructor.
        /// </summary>
        static RemoteRaceInstrumentationEngine()
        {
            Assemblies = new SafeDictionary<string, Assembly>();
        }

        #endregion

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
            new RaceInstrumentationEngine(testingEngine, configuration);

            this.TryLoadReferencedAssemblies(new[] { assembly });
            
            testingEngine.Run();

            IO.Error.PrintLine(testingEngine.Report());
            if (testingEngine.TestReport.NumOfFoundBugs > 0 ||
                configuration.PrintTrace)
            {
                testingEngine.TryEmitTraces();
            }

            if (configuration.ReportCodeCoverage)
            {
                testingEngine.TryEmitCoverageReport();
            }
        }

        public override object InitializeLifetimeService()
        {
            return null;
        }

        #endregion

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

        #endregion
    }
}
