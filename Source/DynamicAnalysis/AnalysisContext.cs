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
    /// The P# static analysis context.
    /// </summary>
    public static class AnalysisContext
    {
        #region fields

        /// <summary>
        /// The P# assembly to analyze.
        /// </summary>
        internal static Assembly Assembly;

        /// <summary>
        /// The scheduling strategy to use.
        /// </summary>
        internal static SchedulingStrategy Strategy;

        /// <summary>
        /// A P# test method.
        /// </summary>
        internal static MethodInfo TestMethod;

        #endregion

        #region public API

        /// <summary>
        /// Create a new P# dynamic analysis context from the given assembly.
        /// </summary>
        /// <param name="assemblyName">Assembly name</param>
        public static void Create(string assemblyName)
        {
            try
            {
                AnalysisContext.Assembly = Assembly.LoadFrom(assemblyName);
            }
            catch (FileNotFoundException ex)
            {
                ErrorReporter.ReportAndExit(ex.Message);
            }

            AnalysisContext.Setup();
        }

        /// <summary>
        /// Create a new P# dynamic analysis context from the given assembly.
        /// </summary>
        /// <param name="assemblyName">Assembly name</param>
        public static void Create(Assembly assembly)
        {
            try
            {
                AnalysisContext.Assembly = assembly;
            }
            catch (FileNotFoundException ex)
            {
                ErrorReporter.ReportAndExit(ex.Message);
            }

            AnalysisContext.Setup();
        }

        #endregion

        #region private methods

        /// <summary>
        /// Setups the analysis context.
        /// </summary>
        private static void Setup()
        {
            AnalysisContext.SetupSchedulingStrategy();
            AnalysisContext.FindEntryPoint();
        }

        /// <summary>
        /// Setups the scheduling strategy.
        /// </summary>
        private static void SetupSchedulingStrategy()
        {
            if (Configuration.SchedulingStrategy.Equals("") ||
                Configuration.SchedulingStrategy.Equals("random"))
            {
                AnalysisContext.Strategy = SchedulingStrategy.Random;
            }
            else if (Configuration.SchedulingStrategy.Equals("dfs"))
            {
                AnalysisContext.Strategy = SchedulingStrategy.DFS;
            }
        }

        /// <summary>
        /// Finds the entry point to the P# program.
        /// </summary>
        private static void FindEntryPoint()
        {
            List<MethodInfo> testMethods = null;

            try
            {
                testMethods = AnalysisContext.Assembly.GetTypes().SelectMany(t => t.GetMethods(BindingFlags.Static |
                    BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.InvokeMethod)).
                    Where(m => m.GetCustomAttributes(typeof(Test), false).Length > 0).ToList();
            }
            catch (ReflectionTypeLoadException ex)
            {
                foreach (var le in ex.LoaderExceptions)
                {
                    ErrorReporter.Report(le.Message);
                }

                ErrorReporter.ReportAndExit("Failed to load assembly '{0}'", AnalysisContext.Assembly.FullName);
            }
            catch (Exception ex)
            {
                ErrorReporter.Report(ex.Message);
                ErrorReporter.ReportAndExit("Failed to load assembly '{0}'", AnalysisContext.Assembly.FullName);
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

            AnalysisContext.TestMethod = testMethods[0];
        }

        #endregion
    }
}
