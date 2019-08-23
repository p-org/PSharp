using
namespace Microsoft.PSharp.StaticAnalysis
{
    /// <summary>
    /// Class implementing an error trace step.
    /// </summary>
    internal class ErrorTraceStep
    {
        /// <summary>
        /// The expression.
        /// </summary>
        internal readonly string Expression;

        /// <summary>
        /// The file name.
        /// </summary>
        internal readonly string File;

        /// <summary>
        /// The line number.
        /// </summary>
        internal readonly int Line;

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorTraceStep"/> class.
        /// </summary>
        internal ErrorTraceStep(string expr, string file, int line)
        {
            this.Expression = expr;
            this.File = file;
            this.Line = line;
        }
    }
}
