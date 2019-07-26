// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.IO;
using Microsoft.PSharp.TestingServices.Runtime.Scheduling.Strategies.ProgramAware;
using Microsoft.PSharp.TestingServices.Scheduling.Strategies;
using Microsoft.PSharp.TestingServices.Tracing.Schedule;

namespace Microsoft.PSharp.TestingClientInterface
{
    public static class TestingClientUtils
    {
        public static bool ReadScheduleFileForReplay(string scheduleFileName, out string[] scheduleDump, /*inout*/ Configuration config, out bool isFairSchedule)
        {
            scheduleDump = File.ReadAllLines(scheduleFileName);
            isFairSchedule = false;
            foreach (var line in scheduleDump)
            {
                if (!line.StartsWith("--"))
                {
                    break;
                }

                if (line.Equals("--fair-scheduling"))
                {
                    isFairSchedule = true;
                }
                else if (line.Equals("--cycle-detection"))
                {
                    config.EnableCycleDetection = true;
                }
                else if (line.StartsWith("--liveness-temperature-threshold:"))
                {
                    config.LivenessTemperatureThreshold =
                        int.Parse(line.Substring("--liveness-temperature-threshold:".Length));
                }
                else if (line.StartsWith("--test-method:"))
                {
                    config.TestMethodName =
                        line.Substring("--test-method:".Length);
                }
            }

            return true;
        }

        public static ISchedulingStrategy CreateReplayStrategy(Configuration config, bool isFair, string[] scheduleDump)
        {
            return new ReplayStrategy(config, new ScheduleTrace(scheduleDump), isFair);
        }

        public static ISchedulingStrategy CreateBasicProgramModelBasedStrategy(ISchedulingStrategy childStrategy)
        {
            return new BasicProgramModelBasedStrategy(childStrategy);
        }
    }
}
