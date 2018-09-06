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
        private ILogger DefaultLogger;

        /// <summary>
        /// Installed logger.
        /// </summary>
        private ILogger InstalledLogger;

        /// <summary>
        /// Creates a new logger that converts logs from an <see cref="ILogger"/> to
        /// an <see cref="TestingServices.SchedulingStrategies.ILogger"/>.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        public SchedulingStrategyLogger(Configuration configuration)
        {
            if (configuration.EnableDebugging)
            {
                DefaultLogger = new ConsoleLogger();
            }
            else
            {
                DefaultLogger = new DisposingLogger();
            }

            InstalledLogger = DefaultLogger;
        }

        /// <summary>
        /// Writes the specified string value.
        /// </summary>
        /// <param name="value">Text</param>
        public void Write(string value)
        {
            InstalledLogger.Write(value);
        }

        /// <summary>
        /// Writes the text representation of the specified array of objects.
        /// </summary>
        /// <param name="format">Text</param>
        /// <param name="args">Arguments</param>
        public void Write(string format, params object[] args)
        {
            InstalledLogger.Write(format, args);
        }

        /// <summary>
        /// Writes the specified string value, followed by the
        /// current line terminator.
        /// </summary>
        /// <param name="value">Text</param>
        public void WriteLine(string value)
        {
            InstalledLogger.WriteLine(value);
        }

        /// <summary>
        /// Writes the text representation of the specified array of objects,
        /// followed by the current line terminator.
        /// </summary>
        /// <param name="format">Text</param>
        /// <param name="args">Arguments</param>
        public void WriteLine(string format, params object[] args)
        {
            InstalledLogger.WriteLine(format, args);
        }

        /// <summary>
        /// Installs the specified <see cref="ILogger"/>.
        /// </summary>
        /// <param name="logger">ILogger</param>
        internal void SetLogger(ILogger logger)
        {
            InstalledLogger = logger;
        }

        /// <summary>
        /// Resets the installed <see cref="ILogger"/> to the default logger.
        /// </summary>
        internal void ResetToDefaultLogger()
        {
            InstalledLogger = DefaultLogger;
        }
    }
}
