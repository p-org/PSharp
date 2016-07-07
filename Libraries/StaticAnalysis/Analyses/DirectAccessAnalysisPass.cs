//-----------------------------------------------------------------------
// <copyright file="DirectAccessAnalysisPass.cs">
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
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.DataFlowAnalysis;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.StaticAnalysis
{
    /// <summary>
    /// This analysis pass checks if any P# machine contains fields
    /// or methods that can be publicly accessed.
    /// </summary>
    internal sealed class DirectAccessAnalysisPass : StateMachineAnalysisPass
    {
        #region internal API

        /// <summary>
        /// Creates a new direct access analysis pass.
        /// </summary>
        /// <param name="context">AnalysisContext</param>
        /// <param name="configuration">Configuration</param>
        /// <returns>DirectAccessAnalysisPass</returns>
        internal static DirectAccessAnalysisPass Create(AnalysisContext context,
            Configuration configuration)
        {
            return new DirectAccessAnalysisPass(context, configuration);
        }

        /// <summary>
        /// Runs the analysis on the specified machines.
        /// </summary>
        /// <param name="machines">StateMachines</param>
        internal override void Run(ISet<StateMachine> machines)
        {
            this.CheckFields(machines);
            this.CheckMethods(machines);
        }

        #endregion

        #region private methods

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="context">AnalysisContext</param>
        /// <param name="configuration">Configuration</param>
        private DirectAccessAnalysisPass(AnalysisContext context, Configuration configuration)
            : base(context, configuration)
        {

        }

        /// <summary>
        /// Checks the fields of each machine and report warnings if
        /// any field is not private or protected.
        /// </summary>
        /// <param name="machines">StateMachines</param>
        private void CheckFields(ISet<StateMachine> machines)
        {
            foreach (var machine in machines)
            {
                foreach (var field in machine.Declaration.ChildNodes().OfType<FieldDeclarationSyntax>())
                {
                    if (field.Modifiers.Any(SyntaxKind.PublicKeyword))
                    {
                        AnalysisErrorReporter.ReportWarning("Field '{0}' of machine '{1}' is declared " +
                            "as 'public'.", field.Declaration.ToString(), machine.Name);
                    }
                    else if (field.Modifiers.Any(SyntaxKind.InternalKeyword))
                    {
                        AnalysisErrorReporter.ReportWarning("Field '{0}' of machine '{1}' is declared " +
                            "as 'internal'.", field.Declaration.ToString(), machine.Name);
                    }
                }
            }
        }

        /// <summary>
        /// Checks the methods of each machine and report warnings if
        /// any method is directly accessed by anything else than the
        /// P# runtime.
        /// </summary>
        /// <param name="machines">StateMachines</param>
        private void CheckMethods(ISet<StateMachine> machines)
        {
            foreach (var machine in machines)
            {
                foreach (var method in machine.Declaration.ChildNodes().OfType<MethodDeclarationSyntax>())
                {
                    if (method.Modifiers.Any(SyntaxKind.PublicKeyword))
                    {
                        AnalysisErrorReporter.ReportWarning("Method '{0}' of machine '{1}' is " +
                            "declared as 'public'.", method.Identifier.ValueText,
                            machine.Name);
                    }
                    else if (method.Modifiers.Any(SyntaxKind.InternalKeyword))
                    {
                        AnalysisErrorReporter.ReportWarning("Method '{0}' of machine '{1}' is " +
                            "declared as 'internal'.", method.Identifier.ValueText,
                            machine.Name);
                    }
                }
            }
        }

        #endregion

        #region profiling methods

        /// <summary>
        /// Prints profiling results.
        /// </summary>
        protected override void PrintProfilingResults()
        {
            IO.PrintLine("... Direct access analysis runtime: '" +
                base.Profiler.Results() + "' seconds.");
        }

        #endregion
    }
}
