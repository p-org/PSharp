using Microsoft.PSharp.LanguageServices.Compilation;

namespace Microsoft.PSharp.LanguageServices.Parsing
{
    /// <summary>
    /// A P# rewriting engine.
    /// </summary>
    public sealed class RewritingEngine
    {
        /// <summary>
        /// The compilation context.
        /// </summary>
        private readonly CompilationContext CompilationContext;

        /// <summary>
        /// Creates a P# rewriting engine.
        /// </summary>
        public static RewritingEngine Create(CompilationContext context) => new RewritingEngine(context);

        /// <summary>
        /// Runs the P# rewriting engine.
        /// </summary>
        public RewritingEngine Run()
        {
            // Rewrite the projects for the active compilation target.
            for (int idx = 0; idx < this.CompilationContext.GetProjects().Count; idx++)
            {
                this.CompilationContext.GetProjects()[idx].Rewrite();
            }

            return this;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RewritingEngine"/> class.
        /// </summary>
        private RewritingEngine(CompilationContext context)
        {
            this.CompilationContext = context;
        }
    }
}
