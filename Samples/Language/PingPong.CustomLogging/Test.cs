// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Microsoft.PSharp;
using Microsoft.PSharp.IO;

namespace PingPong.CustomLogging
{
    /// <summary>
    /// A simple PingPong application written using the P# high-level syntax.
    ///
    /// The P# runtime starts by creating the P# machine 'NetworkEnvironment'. The
    /// 'NetworkEnvironment' machine then creates a 'Server' and a 'Client' machine,
    /// which then communicate by sending 'Ping' and 'Pong' events to each other for
    /// a limited amount of turns.
    ///
    /// This sample shows how to install a custom logger during testing.
    ///
    /// Note: this is an abstract implementation aimed primarily to showcase the testing
    /// capabilities of P#.
    /// </summary>
    public class Program
    {
        static void Main(string[] args)
        {
            // Optional: increases verbosity level to see the P# runtime log.
            var configuration = Configuration.Create().WithVerbosityEnabled(2);

            // Creates a new P# runtime instance, and passes an optional configuration.
            var runtime = PSharpRuntime.Create(configuration);

            // Creates and installs a custom logger.
            ILogger myLogger = new MyLogger();
            runtime.SetLogger(myLogger);

            // Executes the P# program.
            Program.Execute(runtime);

            // Disposes the logger.
            myLogger.Dispose();

            // The P# runtime executes asynchronously, so we wait
            // to not terminate the process.
            Console.WriteLine("Press Enter to terminate...");
            Console.ReadLine();
        }

        /// <summary>
        /// Execute a test iteration (called repeatedly for
        /// each testing iteration).
        /// </summary>
        /// <param name="runtime"></param>
        [Microsoft.PSharp.Test]
        public static void Execute(IMachineRuntime runtime)
        {
            // Assigns a user-defined name to this network environment machine.
            runtime.CreateMachine(typeof(NetworkEnvironment), "TheUltimateNetworkEnvironmentMachine");
        }
    }

    /// <summary>
    /// Custom logger that just dumps to console.
    /// </summary>
    class MyLogger : MachineLogger
    {
        /// <summary>
        /// Writes the specified string value.
        /// </summary>
        /// <param name="value">Text</param>
        public override void Write(string value)
        {
            Console.Write(value);
        }

        /// <summary>
        /// Writes the text representation of the specified array of objects.
        /// </summary>
        /// <param name="format">Text</param>
        /// <param name="args">Arguments</param>
        public override void Write(string format, params object[] args)
        {
            Console.Write($"MyLogger: {format}", args);
        }

        /// <summary>
        /// Writes the specified string value, followed by the
        /// current line terminator.
        /// </summary>
        /// <param name="value">Text</param>
        public override void WriteLine(string value)
        {
            Console.WriteLine($"MyLogger: {value}");
        }

        /// <summary>
        /// Writes the text representation of the specified array of objects,
        /// followed by the current line terminator.
        /// </summary>
        /// <param name="format">Text</param>
        /// <param name="args">Arguments</param>
        public override void WriteLine(string format, params object[] args)
        {
            Console.WriteLine($"MyLogger: {format}", args);
        }

        /// <summary>
        /// Disposes the logger.
        /// </summary>
        public override void Dispose() { }
    }
}
