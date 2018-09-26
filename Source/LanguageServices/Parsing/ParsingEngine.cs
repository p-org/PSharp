// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Linq;

using Microsoft.PSharp.LanguageServices.Compilation;

namespace Microsoft.PSharp.LanguageServices.Parsing
{
    /// <summary>
    /// A P# parsing engine.
    /// </summary>
    public sealed class ParsingEngine
    {
        #region fields

        /// <summary>
        /// The compilation context.
        /// </summary>
        private CompilationContext CompilationContext;

        /// <summary>
        /// The parsing options.
        /// </summary>
        private ParsingOptions Options;

        #endregion

        #region public API

        /// <summary>
        /// Creates a P# parsing engine for the specified compilation
        /// context and using the default parsing options.
        /// </summary>
        /// <param name="context">CompilationContext</param>
        /// <returns>ParsingEngine</returns>
        public static ParsingEngine Create(CompilationContext context)
        {
            return new ParsingEngine(context, ParsingOptions.CreateDefault());
        }

        /// <summary>
        /// Creates a P# parsing engine for the specified compilation
        /// context and using the specified parsing options.
        /// </summary>
        /// <param name="context">CompilationContext</param>
        /// <param name="options">ParsingOptions</param>
        /// <returns>ParsingEngine</returns>
        public static ParsingEngine Create(CompilationContext context, ParsingOptions options)
        {
            return new ParsingEngine(context, options);
        }

        /// <summary>
        /// Runs the P# parsing engine.
        /// </summary>
        /// <returns>ParsingEngine</returns>
        public ParsingEngine Run()
        {
            // Parse the projects.
            if (this.CompilationContext.Configuration.ProjectName.Equals(""))
            {
                foreach (var project in this.CompilationContext.GetSolution().Projects)
                {
                    var psharpProject = new PSharpProject(this.CompilationContext, project.Name);
                    psharpProject.Parse(this.Options);
                    this.CompilationContext.GetProjects().Add(psharpProject);
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

                    var psharpProject = new PSharpProject(this.CompilationContext, project.Name);
                    psharpProject.Parse(this.Options);
                    this.CompilationContext.GetProjects().Add(psharpProject);
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
        /// <param name="options">ParsingOptions</param>
        private ParsingEngine(CompilationContext context, ParsingOptions options)
        {
            this.CompilationContext = context;
            this.Options = options;
        }

        #endregion
    }
}
