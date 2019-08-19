// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.PSharp.IO;
using Microsoft.PSharp.TestingServices.Runtime.Scheduling.ProgramAwareScheduling;
using Microsoft.PSharp.TestingServices.Runtime.Scheduling.ProgramAwareScheduling.ProgramModel;
using Microsoft.PSharp.TestingServices.Runtime.Scheduling.Strategies.ProgramAware;
using Microsoft.PSharp.TestingServices.Scheduling;
using Microsoft.PSharp.TestingServices.Scheduling.Strategies;
using Microsoft.PSharp.TestingServices.Tracing.Schedule;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.TestingClientInterface
{
    public static class TestingClientUtils
    {
        public static bool ReadSummaryFileForReplay(string scheduleFileName, Assembly assembly, out ProgramModelSummary summary, /*inout*/ Configuration config, out bool isSchedulerFair)
        {
            XDocument xmlDoc = XDocument.Load(scheduleFileName);
            XElement xmlSummary = xmlDoc.Element("ProgramSummary");
            XElement stepXml = xmlSummary.Element("Steps");
            List<ProgramStep> orderedSteps = PartialOrderManipulationUtils.DeserializeStepsXmlToSchedule(stepXml, assembly);

            XElement metaXml = xmlSummary.Element("Meta");
            string bugTriggeringIndexStr = metaXml.Element("BugTriggeringStepIndex")?.Value ?? string.Empty;
            ProgramStep bugTriggeringStep = string.IsNullOrEmpty(bugTriggeringIndexStr) ? null : orderedSteps[int.Parse(bugTriggeringIndexStr)];

            List<ProgramStep> withHeldSends = metaXml.Element("WithHeldSendIndices").Elements("WithHeldSendIndex").Select(x => orderedSteps[int.Parse(x.Value)]).ToList();

            string livenessMonitorTypeName = metaXml.Element("LivenessViolatingMonitorType")?.Value ?? string.Empty;
            Type livenessViolatingMonitorType = string.IsNullOrEmpty(livenessMonitorTypeName) ? null : assembly.GetType(livenessMonitorTypeName);
            bool isLivenessBug = bool.Parse(metaXml.Element("IsLivenessBug").Value);
            isSchedulerFair = isLivenessBug;

            summary = new ProgramModelSummary(orderedSteps[0], bugTriggeringStep, withHeldSends, orderedSteps.Count, livenessViolatingMonitorType);

            if (isLivenessBug)
            {
                config.LivenessTemperatureThreshold = bugTriggeringStep.TotalOrderingIndex * 10;
                config.MaxFairSchedulingSteps = orderedSteps.Count + config.LivenessTemperatureThreshold;
                config.MaxUnfairSchedulingSteps = orderedSteps.Count;
            }
            else
            {
                config.MaxFairSchedulingSteps = orderedSteps.Count;
                config.MaxUnfairSchedulingSteps = orderedSteps.Count;
            }

            return true;
        }

        public static bool ReadScheduleFileForReplay(string summaryFileName, out string[] scheduleDump, /*inout*/ Configuration config, out bool isFairSchedule)
        {
            scheduleDump = File.ReadAllLines(summaryFileName);
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

        public static IRandomNumberGenerator GetRandomNumberGenerator(int? seed)
        {
            if (seed == null)
            {
                seed = DateTime.Now.Millisecond;
            }

            return new DefaultRandomNumberGenerator((int)seed);
        }

        public static ISchedulingStrategy CreateStrategyFromOptionString(string strategyString, int maxTotalSteps, int? seed)
        {
            string[] frags = strategyString.Split(new char[] { ':' });
            IRandomNumberGenerator randGen = GetRandomNumberGenerator(seed);

            int maxUnfairSteps = maxTotalSteps / 11;
            int maxFairSteps = maxTotalSteps - maxUnfairSteps;

            switch (frags[0])
            {
                case "random":
                    return new RandomStrategy(maxTotalSteps);

                case "pct":
                    return new PCTStrategy(maxTotalSteps, int.Parse(frags[1]));
                case "fairpct":
                    return new ComboStrategy(
                        new PCTStrategy(maxUnfairSteps, int.Parse(frags[1])),
                        new RandomStrategy(maxFairSteps));

                case "dpor":
                    return new DPORStrategy(null, -1, maxTotalSteps);
                case "rdpor":
                    return new DPORStrategy(randGen, -1, maxTotalSteps);

                default:
                    throw new ArgumentException("Unrecognized strategy: " + strategyString);
            }
        }
    }
}
