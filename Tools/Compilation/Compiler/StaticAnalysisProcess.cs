using Microsoft.PSharp.IO;
using Microsoft.PSharp.LanguageServices.Compilation;
using Microsoft.PSharp.StaticAnalysis;

namespace Microsoft.PSharp
{
    /// <summary>
    /// A P# static analysis process.
    /// </summary>
    internal sealed class StaticAnalysisProcess
    {
        /// <summary>
        /// The compilation context.
        /// </summary>
        private readonly CompilationContext CompilationContext;

        /// <summary>
        /// Creates a P# static analysis process.
        /// </summary>
        public static StaticAnalysisProcess Create(CompilationContext context)
        {
            return new StaticAnalysisProcess(context);
        }

        /// <summary>
        /// Starts the P# static analysis process.
        /// </summary>
        public void Start()
        {
            Output.WriteLine(". Analyzing");

            // Creates and runs a P# static analysis engine.
            var engine = StaticAnalysisEngine.Create(this.CompilationContext).Run();

            if (engine.ErrorReporter.ErrorCount > 0 ||
                (this.CompilationContext.Configuration.ShowWarnings &&
                engine.ErrorReporter.WarningCount > 0))
            {
                Error.ReportAndExit(engine.ErrorReporter.GetStats());
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StaticAnalysisProcess"/> class.
        /// </summary>
        private StaticAnalysisProcess(CompilationContext context)
        {
            this.CompilationContext = context;
        }
    }
}
