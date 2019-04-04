// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Microsoft.PSharp;

namespace PingPong.AsyncAwait
{
    /// <summary>
    /// A simple PingPong application written using the P# high-level syntax.
    ///
    /// The P# runtime starts by creating the P# machine 'NetworkEnvironment'. The
    /// 'NetworkEnvironment' machine then creates a 'Server' and a 'Client' machine,
    /// which then communicate by sending 'Ping' and 'Pong' events to each other for
    /// a limited amount of turns.
    ///
    /// The P# compiler rewrites '.psharp' files to an intermediate C# representation
    /// (see the 'PingPong.PSharpLibrary' sample) before invoking the Roslyn compiler.
    ///
    /// Note: this is an abstract implementation aimed primarily to showcase the testing
    /// capabilities of P#.
    /// </summary>
    public class Program
    {
        public static void Main(string[] args)
        {
            // Optional: increases verbosity level to see the P# runtime log.
            var configuration = Configuration.Create().WithVerbosityEnabled(2);

            // Creates a new P# runtime instance, and passes an optional configuration.
            var runtime = PSharpRuntime.Create(configuration);

            // Executes the P# program.
            Program.Execute(runtime);

            // The P# runtime executes asynchronously, so we wait
            // to not terminate the process.
            Console.WriteLine("Press Enter to terminate...");
            Console.ReadLine();
        }

        /// <summary>
        /// The P# testing engine uses a method annotated with the 'Microsoft.PSharp.Test'
        /// attribute as an entry point.
        ///
        /// During testing, the testing engine takes control of the underlying scheduler
        /// and any declared in P# sources of non-determinism (e.g. P# asynchronous APIs,
        /// P# non-determinstic choices) and systematically executes the test method a user
        /// specified number of iterations to detect bugs.
        /// </summary>
        /// <param name="runtime">PSharpRuntime</param>
        [Microsoft.PSharp.Test]
        public static void Execute(IMachineRuntime runtime)
        {
            // This is the root machine to the P# PingPong program. CreateMachine
            // executes asynchronously (i.e. non-blocking).
            runtime.CreateMachine(typeof(NetworkEnvironment));
        }
    }
}
