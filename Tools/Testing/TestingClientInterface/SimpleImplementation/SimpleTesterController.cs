// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PSharp.TestingClientInterface.SimpleImplementation;
using Microsoft.PSharp.TestingServices;
using Microsoft.PSharp.TestingServices.Scheduling.Strategies;

namespace Microsoft.PSharp.TestingClientInterface
{
    public class SimpleTesterController : AbstractStrategyController
    {
        private readonly IMetricReporter Reporter;

        private readonly ISchedulingStrategy Strategy;

        public SimpleTesterController(Configuration config, ISchedulingStrategy strategy, IMetricReporter reporter)
            : base(config)
        {
            this.Reporter = reporter;
            this.Strategy = strategy;
        }

        public override string GetReport()
        {
            return this.Reporter.GetReport();
        }

        public override void NotifySchedulingEnded(bool bugFound)
        {
            this.Reporter.RecordIteration(this.Strategy, bugFound);
        }

        public override void Initialize(out ISchedulingStrategy strategy)
        {
            strategy = this.Strategy;
        }

        public override bool StrategyPrepareForNextIteration(out ISchedulingStrategy nextStrategy, out int maxSteps)
        {
            nextStrategy = this.Strategy;
            maxSteps = this.Configuration.MaxUnfairSchedulingSteps;
            return this.Strategy.PrepareForNextIteration();
        }

        public override void StrategyReset()
        {
            this.Strategy.Reset();
        }

        // Interface for lazy people like me to call this from their apps.

        public static void RunSimple(ISchedulingStrategy strategy, IMetricReporter reporter, string assemblyToBeAnalyzed, string testMethod, int iterations, int maxUnfairSteps, bool explore = false, int verbosity = 0)
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += new UnhandledExceptionEventHandler(UnhandledExceptionHandler);

            Configuration config = GetConfiguration(assemblyToBeAnalyzed, testMethod, iterations, maxUnfairSteps, explore, verbosity);

            SimpleTesterController controller = new SimpleTesterController(config, strategy, reporter);
            TestingClient testInterface = new TestingClient(controller);
            testInterface.Run();
            IO.Output.WriteLine(reporter.GetReport());
        }

        private static Configuration GetConfiguration(string assemblyToBeAnalyzed, string testMethod, int iterations, int maxUnfairSteps, bool explore = false, int verbosity = 0)
        {
            Configuration config = CreateDefaultConfiguration();

            config.AssemblyToBeAnalyzed = assemblyToBeAnalyzed != null ? assemblyToBeAnalyzed : string.Empty;
            config.TestMethodName = testMethod != null ? testMethod : string.Empty;
            config.SchedulingIterations = iterations;
            config.MaxUnfairSchedulingSteps = maxUnfairSteps;
            config.MaxFairSchedulingSteps = maxUnfairSteps * 10;
            config.PerformFullExploration = explore;
            config.IsVerbose = verbosity > 0;

            return config;
        }

        // Test utils

        public static Exception CaughtException { get; private set; }

        public static bool RunTest(Action<IMachineRuntime> test, ISchedulingStrategy strategy, IMetricReporter reporter, int iterations, int maxUnfairSteps, bool explore = false, int verbosity = 0)
        {
            try
            {
                CaughtException = null;
                Configuration config = GetConfiguration(null, null, iterations, maxUnfairSteps, explore, verbosity);

                SimpleTesterController controller = new SimpleTesterController(config, strategy, reporter);
                TestingClient testInterface = new TestingClient(controller, test);

                testInterface.Run();
            }
            catch (Exception e)
            {
                CaughtException = e;
                IO.Error.Report($"Caught {e.GetType()}: {e.Message}");
                if (e.InnerException != null)
                {
                    IO.Error.Report($"\tInnerException {e.InnerException.GetType()}: {e.InnerException.Message}");
                }

                return false;
            }

            return true;
        }

        private static readonly char[] TrimChars = new char[2] { '-', '/' };
        private static readonly char[] SplitChars = new char[] { ':' };

        public static Dictionary<string, string> ParseCommandlineArgs(string[] args)
        {
            Dictionary<string, string> argMap = new Dictionary<string, string>();
            foreach (string s in args)
            {
                string[] kv = s.Trim(TrimChars).Split(SplitChars, 2);
                argMap.Add(kv[0], kv.Length > 1 ? kv[1] : null);
            }

            return argMap;
        }

        private static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs args)
        {
            var ex = (Exception)args.ExceptionObject;
            IO.Error.Report("[PSharpTester] internal failure: {0}: {1}", ex.GetType().ToString(), ex.Message);
            IO.Output.WriteLine(ex.StackTrace);
            Environment.Exit(1);
        }
    }
}
