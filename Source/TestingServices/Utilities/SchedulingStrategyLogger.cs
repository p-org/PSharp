// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

namespace Microsoft.PSharp.IO
{
    /// <summary>
    /// Logger for scheduling strategies. This is a converter from an <see cref="ILogger"/> to
    /// an <see cref="TestingServices.SchedulingStrategies.ILogger"/>. If debugging is enabled,
    /// it uses the <see cref="ConsoleLogger"/>, or the <see cref="DisposingLogger"/> if
    /// debugging is disabled.
    /// </summary>
    internal sealed class SchedulingStrategyLogger : TestingServices.SchedulingStrategies.ILogger
    {
        /// <summary>
        /// Default logger.
        /// </summary>
        private readonly ILogger DefaultLogger;

        /// <summary>
        /// Installed logger.
        /// </summary>
        private ILogger InstalledLogger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SchedulingStrategyLogger"/> class.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        public SchedulingStrategyLogger(Configuration configuration)
        {
            if (configuration.EnableDebugging)
            {
                this.DefaultLogger = new ConsoleLogger();
            }
            else
            {
                this.DefaultLogger = new DisposingLogger();
            }

            this.InstalledLogger = this.DefaultLogger;
        }

        /// <summary>
        /// Writes the specified string value.
        /// </summary>
        public void Write(string value)
        {
            this.InstalledLogger.Write(value);
        }

        /// <summary>
        /// Writes the text representation of the specified array of objects.
        /// </summary>
        public void Write(string format, params object[] args)
        {
            this.InstalledLogger.Write(format, args);
        }

        /// <summary>
        /// Writes the specified string value, followed by the
        /// current line terminator.
        /// </summary>
        public void WriteLine(string value)
        {
            this.InstalledLogger.WriteLine(value);
        }

        /// <summary>
        /// Writes the text representation of the specified array of objects,
        /// followed by the current line terminator.
        /// </summary>
        public void WriteLine(string format, params object[] args)
        {
            this.InstalledLogger.WriteLine(format, args);
        }

        /// <summary>
        /// Installs the specified <see cref="ILogger"/>.
        /// </summary>
        internal void SetLogger(ILogger logger)
        {
            this.InstalledLogger = logger;
        }

        /// <summary>
        /// Resets the installed <see cref="ILogger"/> to the default logger.
        /// </summary>
        internal void ResetToDefaultLogger()
        {
            this.InstalledLogger = this.DefaultLogger;
        }
    }
}
