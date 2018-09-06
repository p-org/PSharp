// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

using Microsoft.PSharp.IO;

namespace Microsoft.PSharp.Utilities
{
    /// <summary>
    /// Reports errors and warnings to the user.
    /// </summary>
    public sealed class ErrorReporter
    {
        #region fields

        /// <summary>
        /// Configuration.
        /// </summary>
        private Configuration Configuration;

        #endregion

        #region properties

        /// <summary>
        /// The installed logger.
        /// </summary>
        internal ILogger Logger { get; set; }

        #endregion

        #region constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="logger">ILogger</param>
        internal ErrorReporter(Configuration configuration, ILogger logger)
        {
            this.Configuration = configuration;
            this.Logger = logger ?? new ConsoleLogger();
        }

        #endregion

        #region public methods

        /// <summary>
        /// Reports an error, followed by the current line terminator.
        /// </summary>
        /// <param name="value">Text</param>
        public void WriteErrorLine(string value)
        {
            this.Write("Error: ", ConsoleColor.Red);
            this.Write(value, ConsoleColor.Yellow);
            this.Logger.WriteLine("");
        }

        /// <summary>
        /// Reports a warning, followed by the current line terminator.
        /// </summary>
        /// <param name="value">Text</param>
        public void WriteWarningLine(string value)
        {
            if (this.Configuration.ShowWarnings)
            {
                this.Write("Warning: ", ConsoleColor.Red);
                this.Write(value, ConsoleColor.Yellow);
                this.Logger.WriteLine("");
            }
        }

        #endregion

        #region private methods

        /// <summary>
        /// Writes the specified string value.
        /// </summary>
        /// <param name="value">Text</param>
        /// <param name="color">ConsoleColor</param>
        private void Write(string value, ConsoleColor color)
        {
            ConsoleColor previousForegroundColor = default(ConsoleColor);
            if (this.Configuration.EnableColoredConsoleOutput)
            {
                previousForegroundColor = Console.ForegroundColor;
                Console.ForegroundColor = color;
            }

            this.Logger.Write(value);

            if (this.Configuration.EnableColoredConsoleOutput)
            {
                Console.ForegroundColor = previousForegroundColor;
            }
        }

        #endregion
    }
}
