// ------------------------------------------------------------------------------------------------

namespace Microsoft.PSharp.StaticAnalysis.Tests
{
    internal static class Setup
    {
        internal static Configuration GetConfiguration()
        {
            var configuration = Configuration.Create();
            configuration.ProjectName = "Test";
            configuration.ThrowInternalExceptions = true;
            configuration.IsVerbose = true;
            configuration.AnalyzeDataFlow = true;
            configuration.AnalyzeDataRaces = true;
            return configuration;
        }
    }
}
