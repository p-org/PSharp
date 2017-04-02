//-----------------------------------------------------------------------
// <copyright file="StaticAnalysisEngine.cs">
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
using Microsoft.CodeAnalysis.CSharp.DataFlowAnalysis;
using Microsoft.PSharp.IO;
using Microsoft.PSharp.LanguageServices.Compilation;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.StaticAnalysis
{
    /// <summary>
    /// A P# static analysis engine.
    /// </summary>
    public sealed class StaticAnalysisEngine
    {
        #region fields

        /// <summary>
        /// The compilation context.
        /// </summary>
        private CompilationContext CompilationContext;

        /// <summary>
        /// The installed logger.
        /// </summary>
        private ILogger Logger;

        /// <summary>
        /// The overall runtime profiler.
        /// </summary>
        private Profiler Profiler;

        #endregion

        #region properties

        /// <summary>
        /// The error reporter.
        /// </summary>
        internal ErrorReporter ErrorReporter { get; private set; }

        #endregion

        #region public API

        /// <summary>
        /// Creates a P# static analysis engine.
        /// </summary>
        /// <param name="context">CompilationContext</param>
        /// <returns>StaticAnalysisEngine</returns>
        public static StaticAnalysisEngine Create(CompilationContext context)
        {
            return new StaticAnalysisEngine(context, new ConsoleLogger());
        }

        /// <summary>
        /// Creates a P# static analysis engine.
        /// </summary>
        /// <param name="context">CompilationContext</param>
        /// <param name="logger">ILogger</param>
        /// <returns>StaticAnalysisEngine</returns>
        public static StaticAnalysisEngine Create(CompilationContext context, ILogger logger)
        {
            return new StaticAnalysisEngine(context, logger);
        }

        /// <summary>
        /// Runs the P# static analysis engine.
        /// </summary>
        /// <returns>StaticAnalysisEngine</returns>
        public StaticAnalysisEngine Run()
        {
            // Parse the projects.
            if (this.CompilationContext.Configuration.ProjectName.Equals(""))
            {
                foreach (var project in this.CompilationContext.GetSolution().Projects)
                {
                    this.AnalyzeProject(project);
                }
            }
            else
            {
                // Find the project specified by the user.
                var targetProject = this.CompilationContext.GetSolution().Projects.Where(
                    p => p.Name.Equals(this.CompilationContext.Configuration.ProjectName)).FirstOrDefault();

                var projectDependencyGraph = this.CompilationContext.GetSolution().GetProjectDependencyGraph();
                var projectDependencies = projectDependencyGraph.GetProjectsThatThisProjectTransitivelyDependsOn(targetProject.Id);

                foreach (var project in this.CompilationContext.GetSolution().Projects)
                {
                    if (!projectDependencies.Contains(project.Id) && !project.Id.Equals(targetProject.Id))
                    {
                        continue;
                    }
                    
                    this.AnalyzeProject(project);
                }
            }

            return this;
        }

        #endregion

        #region constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="context">CompilationContext</param>
        /// <param name="logger">ILogger</param>
        private StaticAnalysisEngine(CompilationContext context, ILogger logger)
        {
            this.ErrorReporter = new ErrorReporter(context.Configuration, logger);
            this.Logger = logger ?? new ConsoleLogger();
            this.Profiler = new Profiler();
            this.CompilationContext = context;
        }

        #endregion

        #region private methods

        /// <summary>
        /// Analyzes the given P# project.
        /// </summary>
        /// <param name="project">Project</param>
        private void AnalyzeProject(Project project)
        {
            // Starts profiling the analysis.
            if (this.CompilationContext.Configuration.EnableProfiling)
            {
                this.Profiler.StartMeasuringExecutionTime();
            }

            // Create a state-machine static analysis context.
            var context = AnalysisContext.Create(project);
            this.PerformErrorChecking(context);

            this.RegisterImmutableTypes(context);
            this.RegisterGivesUpOwnershipOperations(context);

            // Creates and runs an analysis pass that computes the
            // summaries for every P# machine.
            ISet<StateMachine> machines = new HashSet<StateMachine>();

            try
            {
                // Creates summaries for each machine, which can be used for subsequent
                // analyses. Optionally performs data-flow analysis.
                MachineSummarizationPass.Create(context, this.CompilationContext.Configuration, this.Logger,
                    this.ErrorReporter).Run(machines);

                // Creates and runs an analysis pass that detects if a machine contains
                // states that are declared as generic. This is not allowed by P#.
                NoGenericStatesAnalysisPass.Create(context, this.CompilationContext.Configuration, this.Logger,
                    this.ErrorReporter).Run(machines);

                // Creates and runs an analysis pass that finds if a machine exposes
                // any fields or methods to other machines.
                DirectAccessAnalysisPass.Create(context, this.CompilationContext.Configuration, this.Logger,
                    this.ErrorReporter).Run(machines);

                if (this.CompilationContext.Configuration.AnalyzeDataRaces)
                {
                    // Creates and runs an analysis pass that detects if any method
                    // in each machine is erroneously giving up ownership.
                    GivesUpOwnershipAnalysisPass.Create(context, this.CompilationContext.Configuration, this.Logger,
                        this.ErrorReporter).Run(machines);

                    // Creates and runs an analysis pass that detects if all methods
                    // in each machine respect given up ownerships.
                    RespectsOwnershipAnalysisPass.Create(context, this.CompilationContext.Configuration, this.Logger,
                        this.ErrorReporter).Run(machines);
                }
            }
            catch (Exception ex)
            {
                if (this.CompilationContext.Configuration.ThrowInternalExceptions)
                {
                    throw ex;
                }

                this.Logger.WriteLine($"... Failed to analyze project '{project.Name}'");
                if (this.CompilationContext.Configuration.EnableDebugging)
                {
                    this.Logger.WriteLine(ex.ToString());
                }
            }
            finally
            {
                // Stops profiling the analysis.
                if (this.CompilationContext.Configuration.EnableProfiling)
                {
                    this.Profiler.StopMeasuringExecutionTime();
                    this.Logger.WriteLine("... Total static analysis runtime: '" +
                        this.Profiler.Results() + "' seconds.");
                }
            }
        }

        /// <summary>
        /// Performs error checking on the P# compilation. It
        /// reports any found diagnostics and exits.
        /// </summary>
        /// <param name="context">AnalysisContext</param>
        /// <returns>GivesUpOwnershipAnalysisPass</returns>
        private void PerformErrorChecking(AnalysisContext context)
        {
            List<Diagnostic> diagnostics = new List<Diagnostic>();
            foreach (var syntaxTree in context.Compilation.SyntaxTrees)
            {
                diagnostics.AddRange(syntaxTree.GetDiagnostics());
            }

            if (diagnostics.Count > 0)
            {
                var message = string.Join("\r\n", diagnostics);

                if (this.CompilationContext.Configuration.ThrowInternalExceptions)
                {
                    throw new Exception(message);
                }

                Error.ReportAndExit(message);
            }
        }

        /// <summary>
        /// Registers immutable types.
        /// </summary>
        /// <param name="context">AnalysisContext</param>
        private void RegisterImmutableTypes(AnalysisContext context)
        {
            context.RegisterImmutableType(typeof(Microsoft.PSharp.MachineId));
        }

        /// <summary>
        /// Registers gives-up ownership operations.
        /// </summary>
        /// <param name="context">AnalysisContext</param>
        private void RegisterGivesUpOwnershipOperations(AnalysisContext context)
        {
            context.RegisterGivesUpOwnershipMethod("Microsoft.PSharp.Send",
                new HashSet<int> { 1 });
            context.RegisterGivesUpOwnershipMethod("Microsoft.PSharp.CreateMachine",
                new HashSet<int> { 1 });
        }

        #endregion
    }
}
