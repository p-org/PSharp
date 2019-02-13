using Microsoft.PSharp.Utilities;
using Microsoft.PSharp.IO;
using System;

namespace Microsoft.PSharp.TestingServices
{
    public class Program
    {
        static void Main(string[] args)
        {
            // Parses the command line options to get the configuration.
            var configuration = new MinimizerCommandLineOptions(args).Parse();

            // Creates and starts a Minimizer process.
            MinimizingProcess.Create(configuration).Start();

            Output.WriteLine(". Done");

        }

    }
}
