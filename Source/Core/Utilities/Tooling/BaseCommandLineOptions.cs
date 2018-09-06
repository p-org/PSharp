﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Microsoft.PSharp.IO;

namespace Microsoft.PSharp.Utilities
{
    /// <summary>
    /// The P# base command line options.
    /// </summary>
    public abstract class BaseCommandLineOptions
    {
        #region fields

        /// <summary>
        /// Configuration.
        /// </summary>
        protected Configuration Configuration;

        /// <summary>
        /// Command line options.
        /// </summary>
        protected string[] Options;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="args">Array of arguments</param>
        public BaseCommandLineOptions(string[] args)
        {
            this.Configuration = Configuration.Create();
            this.Options = args;
        }

        /// <summary>
        /// Parses the command line options and returns a configuration.
        /// </summary>
        /// <returns>Configuration</returns>
        public Configuration Parse()
        {
            for (int idx = 0; idx < this.Options.Length; idx++)
            {
                this.ParseOption(this.Options[idx]);
            }

            this.CheckForParsingErrors();
            this.UpdateConfiguration();
            return Configuration;
        }

        #endregion

        #region protected methods

        /// <summary>
        /// Parses the given option.
        /// </summary>
        /// <param name="option">Option</param>
        protected virtual void ParseOption(string option)
        {
            if (option.ToLower().Equals("/?"))
            {
                this.ShowHelp();
                Environment.Exit(0);
            }
            else if (option.ToLower().StartsWith("/s:") && option.Length > 3)
            {
                this.Configuration.SolutionFilePath = option.Substring(3);
            }
            else if (option.ToLower().StartsWith("/p:") && option.Length > 3)
            {
                this.Configuration.ProjectName = option.Substring(3);
            }
            else if (option.ToLower().StartsWith("/o:") && option.Length > 3)
            {
                this.Configuration.OutputFilePath = option.Substring(3);
            }
            else if (option.ToLower().StartsWith("/v:") && option.Length > 3)
            {
                int i = 0;
                if (!int.TryParse(option.Substring(3), out i) && i > 0 && i <= 3)
                {
                    Error.ReportAndExit("Please give a valid verbosity level " +
                        "'/v:[x]', where 1 <= [x] <= 3.");
                }

                this.Configuration.Verbose = i;
            }
            else if (option.ToLower().Equals("/debug"))
            {
                this.Configuration.EnableDebugging = true;
                Debug.IsEnabled = true;
            }
            else if (option.ToLower().Equals("/warnings-on"))
            {
                this.Configuration.ShowWarnings = true;
            }
            else if (option.ToLower().StartsWith("/timeout:") && option.Length > 9)
            {
                int i = 0;
                if (!int.TryParse(option.Substring(9), out i) &&
                    i > 0)
                {
                    Error.ReportAndExit("Please give a valid timeout '/timeout:[x]', where [x] > 0 seconds.");
                }

                this.Configuration.Timeout = i;
            }
            else
            {
                this.ShowHelp();
                Error.ReportAndExit("cannot recognise command line option '" + option + "'.");
            }
        }

        /// <summary>
        /// Checks for parsing errors.
        /// </summary>
        protected abstract void CheckForParsingErrors();

        /// <summary>
        /// Updates the configuration depending on the
        /// user specified options.
        /// </summary>
        protected abstract void UpdateConfiguration();

        /// <summary>
        /// Shows help.
        /// </summary>
        protected abstract void ShowHelp();

        #endregion
    }
}
