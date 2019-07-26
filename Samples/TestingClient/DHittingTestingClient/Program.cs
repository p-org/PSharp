using Microsoft.PSharp;
using Microsoft.PSharp.TestingServices.Scheduling.Strategies;
using Microsoft.PSharp.TestingClientInterface;
using Microsoft.PSharp.TestingClientInterface.SimpleImplementation;
using System;
using Microsoft.PSharp.TestingServices.Runtime.Scheduling.Strategies.ProgramAware;

namespace DHittingTestingClient
{
    public class Program {
        public static void Main(string[] args)
        {
            if (args.Length < 4)
            {
                Microsoft.PSharp.IO.Error.Report($"Usage: DHittingTestingClient.exe <AssemblyToBeTested> <nIterations> <maxSteps> <StepSignatureType>");
                Microsoft.PSharp.IO.Error.Report($"\tStepSignatureType must be one of: [{string.Join(", ", Enum.GetNames(typeof(DHittingUtils.DHittingSignature)))}]");
                Environment.Exit(1);
            }

            int iters = Int32.Parse(args[1]);
            int maxSteps = Int32.Parse(args[2]);
 

            if (!Enum.TryParse<DHittingUtils.DHittingSignature>(args[3], out DHittingUtils.DHittingSignature stepSignatureType))
            {
                Microsoft.PSharp.IO.Error.Report($"Specified stepSignatureType must be one of:[{string.Join(", ", Enum.GetNames(typeof(DHittingUtils.DHittingSignature)))}]");
            }
            bool shouldHashEvents = (stepSignatureType == DHittingUtils.DHittingSignature.EventHash);

            SimpleTesterController.RunSimple(new BasicProgramModelBasedStrategy(new RandomStrategy(maxSteps*11), true), new InboxBasedDHittingMetricReporter(2, stepSignatureType), args[0], iters, maxSteps, true, 0);
            Console.ReadKey();
        }
    }
}
