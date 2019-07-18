using Microsoft.PSharp;
using Microsoft.PSharp.TestingClientInterface;
using System;

namespace SampleTestingClient
{
    public class Program { 
        public static void Main(string[] args)
        {
            if (args.Length < 4)
            {
                Microsoft.PSharp.IO.Error.Report($"Usage: SampleTestingClient.exe <AssemblyToBeTested> <nIterations> <maxSteps> <nBugsToStopAt>");
                Environment.Exit(1);
            }

            try
            {
                AppDomain currentDomain = AppDomain.CurrentDomain;
                currentDomain.UnhandledException += new UnhandledExceptionEventHandler(UnhandledExceptionHandler);
                Configuration config = CreateConfig(args);
                SampleController controller = new SampleController(config, Int32.Parse(args[3]));
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

            config.AssemblyToBeAnalyzed = args[0];
            config.SchedulingIterations = Int32.Parse(args[1]);
            config.MaxSchedulingSteps = Int32.Parse(args[2]);
            
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
