// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------
#if YOURE_NOT_USING_ABSTRACTSTRATCONTROLLER

using System;
using Microsoft.PSharp.IO;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.TestingClientInterface
{
    internal class ConfigurationWrapper
    {
        internal Configuration 

        public ConfigurationWrapper(TestingClientInterface.InitialConfigurationOptions initialConfig)
            : base(Array.Empty<string>())
        {
            this.CopyInitialConfiguration(initialConfig);
            this.UpdateConfiguration();
            this.CheckForParsingErrors();
        }

        internal Configuration GetConfiguration()
        {
            return this.Configuration;
        }

        internal void UpdateAndValidateConfiguration()
        {
            this.UpdateConfiguration();
            this.CheckForParsingErrors();
        }

        private void CopyInitialConfiguration(TestingClientInterface.InitialConfigurationOptions initialConfig)
        {
            this.Configuration.SchedulingIterations = initialConfig.SchedulingIterations;
            this.Configuration.EnableLivenessChecking = initialConfig.EnableLivenessChecking;
        }

        protected override void CheckForParsingErrors()
        {
            if (string.IsNullOrEmpty(this.Configuration.AssemblyToBeAnalyzed))
            {
                Error.ReportAndExit("Invalid Configuration: AssemblyToBeAnalyzed is empty.");
            }

            if (this.Configuration.SchedulingStrategy != SchedulingStrategy.ControlUnit)
            {
                Error.ReportAndExit("Invalid Configuration: SchedulingStrategy has to be ControlUnit");
            }

            if (this.Configuration.MaxFairSchedulingSteps < this.Configuration.MaxUnfairSchedulingSteps)
            {
                Error.ReportAndExit("Invalid Configuration:  MaxFairSchedulingSteps < MaxUnfairSchedulingSteps");
            }
        }

        protected override void ParseOption(string option)
        {
            throw new NotImplementedException("This is not meant to be used with parsing");
        }

        protected override void ShowHelp()
        {
            // No help
        }

    }
}

#endif
