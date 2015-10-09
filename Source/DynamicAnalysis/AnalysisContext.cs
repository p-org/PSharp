//-----------------------------------------------------------------------
// <copyright file="AnalysisContext.cs">
//      Copyright (c) 2015 Pantazis Deligiannis (p.deligiannis@imperial.ac.uk)
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
using System.IO;
using System.Linq;
using System.Reflection;

using Microsoft.PSharp.Tooling;

namespace Microsoft.PSharp.DynamicAnalysis
{
    /// <summary>
    /// A P# dynamic analysis context.
    /// </summary>
    public sealed class AnalysisContext
    {
        #region fields

        /// <summary>
        /// Configuration.
        /// </summary>
        internal DynamicAnalysisConfiguration Configuration;

        /// <summary>
        /// The P# assembly to analyze.
        /// </summary>
        internal Assembly Assembly;

        /// <summary>
        /// A P# test method.
        /// </summary>
        internal MethodInfo TestMethod;

        #endregion

        #region public API

        /// <summary>
        /// Create a new P# dynamic analysis context from the given assembly name.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="assemblyName">Assembly name</param>
        /// <returns>AnalysisContext</returns>
        public static AnalysisContext Create(DynamicAnalysisConfiguration configuration, string assemblyName)
        {
            return new AnalysisContext(configuration, assemblyName);
        }

        /// <summary>
        /// Create a new P# dynamic analysis context from the given assembly.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="assembly">Assembly</param>
        /// <returns>AnalysisContext</returns>
        public static AnalysisContext Create(DynamicAnalysisConfiguration configuration, Assembly assembly)
        {
            return new AnalysisContext(configuration, assembly);
        }

        #endregion

        #region private methods

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="assemblyName">Assembly name</param>
        private AnalysisContext(DynamicAnalysisConfiguration configuration, string assemblyName)
        {
            this.Configuration = configuration;

            try
            {
                this.Assembly = Assembly.LoadFrom(assemblyName);
            }
            catch (FileNotFoundException ex)
            {
                ErrorReporter.ReportAndExit(ex.Message);
            }

            this.FindEntryPoint();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="assembly">Assembly</param>
        private AnalysisContext(DynamicAnalysisConfiguration configuration, Assembly assembly)
        {
            this.Configuration = configuration;
            this.Assembly = assembly;
            this.FindEntryPoint();
        }

        /// <summary>
        /// Finds the entry point to the P# program.
        /// </summary>
        private void FindEntryPoint()
        {
            List<MethodInfo> testMethods = null;

            try
            {
                testMethods = this.Assembly.GetTypes().SelectMany(t => t.GetMethods(BindingFlags.Static |
                    BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.InvokeMethod)).
                    Where(m => m.GetCustomAttributes(typeof(Test), false).Length > 0).ToList();
            }
            catch (ReflectionTypeLoadException ex)
            {
                foreach (var le in ex.LoaderExceptions)
                {
                    ErrorReporter.Report(le.Message);
                }

                ErrorReporter.ReportAndExit("Failed to load assembly '{0}'", this.Assembly.FullName);
            }
            catch (Exception ex)
            {
                ErrorReporter.Report(ex.Message);
                ErrorReporter.ReportAndExit("Failed to load assembly '{0}'", this.Assembly.FullName);
            }

            if (testMethods.Count == 0)
            {
                ErrorReporter.ReportAndExit("Cannot detect a P# test method. " +
                    "Use the attribute [Test] to declare a test method.");
            }
            else if (testMethods.Count > 1)
            {
                ErrorReporter.ReportAndExit("Only one test method to the P# program can be declared. " +
                    "{0} test methods were found instead.", testMethods.Count);
            }

            if (testMethods[0].ReturnType != typeof(void) ||
                testMethods[0].ContainsGenericParameters ||
                testMethods[0].GetParameters().Length > 0 ||
                testMethods[0].IsAbstract || testMethods[0].IsVirtual ||
                testMethods[0].IsConstructor ||
                !testMethods[0].IsPublic || !testMethods[0].IsStatic)
            {
                ErrorReporter.ReportAndExit("Incorrect test method declaration. Please " +
                    "declare the test method as follows:\n" +
                    "  [Test] public static void Test() { ... }");
            }

            this.TestMethod = testMethods[0];
        }

        #endregion
    }
}
