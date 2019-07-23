using Microsoft.PSharp;
using Microsoft.PSharp.TestingClientInterface;
using System;

namespace PartialOrderSlicingClient
{
    public class Program {
        private static string ScheduleFileToReplay;

        public static void Main(string[] args)
        {
            if (args.Length < 2) // 4)
            {
                Microsoft.PSharp.IO.Error.Report($"Usage: SampleTestingClient.exe <AssemblyToBeTested> <nIterations> <maxSteps> <nBugsToStopAt>");
                Environment.Exit(1);
            }

            try
            {
                AppDomain currentDomain = AppDomain.CurrentDomain;
                currentDomain.UnhandledException += new UnhandledExceptionEventHandler(UnhandledExceptionHandler);
                Configuration config = CreateConfig(args);
                PartialOrderSliceController controller = new PartialOrderSliceController(config, ScheduleFileToReplay);
                TestingClient testInterface = new TestingClient(controller);
                testInterface.Run();
             }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
        }

        private static Configuration CreateConfig(string[] args)
        {
            Configuration config = AbstractStrategyController.CreateDefaultConfiguration();

            config.WithVerbosityEnabled(true);
            config.AssemblyToBeAnalyzed = args[0];
            ScheduleFileToReplay = args[1];

            config.SchedulingIterations = 50; // TODO:
            // config.SchedulingIterations = Int32.Parse(args[1]);
            // config.MaxSchedulingSteps = Int32.Parse(args[2]);


            config.PerformFullExploration = true;
            return config;
        }
        /// <summary>
        /// Handler for unhandled exceptions.
        /// </summary>
        private static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs args)
        {
            var ex = (Exception)args.ExceptionObject;
            Microsoft.PSharp.IO.Error.Report("[PSharpTester] internal failure: {0}: {1}", ex.GetType().ToString(), ex.Message);
            Microsoft.PSharp.IO.Output.WriteLine(ex.StackTrace);
            Environment.Exit(1);
        }
    }
}
