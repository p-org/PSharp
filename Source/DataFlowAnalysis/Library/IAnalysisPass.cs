using
namespace Microsoft.PSharp.DataFlowAnalysis
{
    /// <summary>
    /// Interface of a generic analysis pass.
    /// </summary>
    public interface IAnalysisPass
    {
        /// <summary>
        /// Runs the analysis.
        /// </summary>
        void Run();
    }
}
