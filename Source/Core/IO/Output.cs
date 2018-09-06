// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

namespace Microsoft.PSharp.IO
{
    /// <summary>
    /// Static class implementing output methods.
    /// </summary>
    public static class Output
    {
        #region fields

        /// <summary>
        /// The underlying logger.
        /// </summary>
        internal static ILogger Logger { get; private set; }

        #endregion

        #region constructors

        /// <summary>
        /// Static constructor.
        /// </summary>
        static Output()
        {
            Logger = new ConsoleLogger();
        }

        #endregion

        #region methods

        /// <summary>
        ///  Writes the specified string value to the output stream.
        /// </summary>
        /// <param name="value">Text</param>
        public static void Write(string value)
        {
            Logger.Write(value);
        }

        /// <summary>
        /// Writes the text representation of the specified array
        /// of objects to the output stream.
        /// </summary>
        public static void Write(string format, params object[] args)
        {
            Logger.Write(format, args);
        }

        /// <summary>
        /// Writes the specified string value, followed by the
        /// current line terminator, to the output stream.
        /// </summary>
        /// <param name="value">Text</param>
        public static void WriteLine(string value)
        {
            Logger.WriteLine(value);
        }

        /// <summary>
        /// Writes the text representation of the specified array
        /// of objects, followed by the current line terminator, to
        /// the output stream.
        /// </summary>
        public static void WriteLine(string format, params object[] args)
        {
            Logger.WriteLine(format, args);
        }

        #endregion

        #region logger management

        /// <summary>
        /// Installs the specified logger. If a null logger is provided,
        /// then the default logger will be installed.
        /// </summary>
        /// <param name="logger">ILogger</param>
        internal static void SetLogger(ILogger logger)
        {
            Logger?.Dispose();
            Logger = logger ?? new ConsoleLogger();
        }

        /// <summary>
        /// Replaces the currently installed logger with the default logger.
        /// </summary>
        internal static void RemoveLogger()
        {
            Logger?.Dispose();
            Logger = new ConsoleLogger();
        }

        #endregion
    }
}
