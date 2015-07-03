//-----------------------------------------------------------------------
// <copyright file="DynamicAnalyzer.cs">
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

using Microsoft.PSharp.DynamicAnalysis;
using Microsoft.PSharp.Tooling;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Dynamic analysis for the P# language.
    /// </summary>
    internal static class DynamicAnalyzer
    {
        /// <summary>
        /// Starts the P# dynamic analyzer.
        /// </summary>
        public static void Run()
        {
            if (!Configuration.RunDynamicAnalysis)
            {
                return;
            }

            foreach (var dll in Configuration.AssembliesToBeAnalyzed)
            {
                Output.Print(". Testing " + dll);
                DynamicAnalyzer.AnalyseAssembly(dll);
            }
        }

        /// <summary>
        /// Analyse the given P# assembly.
        /// </summary>
        /// <param name="dll">Assembly</param>
        private static void AnalyseAssembly(string dll)
        {
            // Create a P# analysis context.
            AnalysisContext.Create(dll);

            // Invokes the systematic testing engine to find bugs
            // in the P# program.
            SCTEngine.Setup();
            SCTEngine.Run();
        }
    }
}
