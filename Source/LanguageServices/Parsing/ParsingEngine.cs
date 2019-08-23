using System.Linq;

using Microsoft.PSharp.LanguageServices.Compilation;

namespace Microsoft.PSharp.LanguageServices.Parsing
{
    /// <summary>
    /// A P# parsing engine.
    /// </summary>
    public sealed class ParsingEngine
    {
        /// <summary>
        /// The compilation context.
        /// </summary>
        private readonly CompilationContext CompilationContext;

        /// <summary>
        /// The parsing options.
        /// </summary>
        private readonly ParsingOptions Options;

        /// <summary>
        /// Creates a P# parsing engine for the specified compilation
        /// context and using the default parsing options.
        /// </summary>
        public static ParsingEngine Create(CompilationContext context)
        {
            return new ParsingEngine(context, ParsingOptions.CreateDefault());
        }

        /// <summary>
        /// Creates a P# parsing engine for the specified compilation
        /// context and using the specified parsing options.
        /// </summary>
        public static ParsingEngine Create(CompilationContext context, ParsingOptions options)
        {
            return new ParsingEngine(context, options);
        }

        /// <summary>
        /// Runs the P# parsing engine.
        /// </summary>
        public ParsingEngine Run()
        {
            // Parse the projects.
            if (string.IsNullOrEmpty(this.CompilationContext.Configuration.ProjectName))
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

        /// <summary>
        /// Initializes a new instance of the <see cref="ParsingEngine"/> class.
        /// </summary>
        private ParsingEngine(CompilationContext context, ParsingOptions options)
        {
            this.CompilationContext = context;
            this.Options = options;
        }
    }
}
