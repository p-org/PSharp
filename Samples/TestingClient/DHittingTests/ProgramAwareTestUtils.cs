using DHittingTestingClient;
using Microsoft.PSharp;
using Microsoft.PSharp.TestingClientInterface;
using Microsoft.PSharp.TestingClientInterface.SimpleImplementation;
using Microsoft.PSharp.TestingServices.Scheduling.Strategies;
using System;
using Xunit;

namespace DHittingTests
{
    // Some utils
    internal class ProgramAwareTestUtils
    {
        public static void RunTest(Action<IMachineRuntime> testAction, ISchedulingStrategy strategy, IMetricReporter metricReporter, int nIterations, int nMaxSteps, bool explore)
        {
            Assert.True(SimpleTesterController.RunTest(testAction, strategy, metricReporter, nIterations, nMaxSteps, explore, 0), "The test encountered an unexpected error:\n" + SimpleTesterController.CaughtException);
        }


        public static void CheckDTupleCount(AbstractDHittingReporter metricReporter, int d, int expectedDTupleCount)
        {
            ulong actualDTupleCount = metricReporter.GetDTupleCount(d);
            Assert.True(
                actualDTupleCount == (ulong)expectedDTupleCount,
                $"Number of expected {d}-tuples did not match. Expected {expectedDTupleCount} ; Received {actualDTupleCount}");
        }

        internal static int Permute(int n, int r)
        {
            int p = 1;
            for (int i = n; i > (n - r); i--)
            {
                p *= i;
            }

            return p;
        }

        internal static int Choose(int n, int r)
        {
            return Permute(n, r) / Permute(r, r);
        }
    }

}
