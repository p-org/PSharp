// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Microsoft.PSharp;

namespace ReplicatingStorage
{
    /// <summary>
    /// A single-process implementation of a replicating storage written using the P#
    /// high-level syntax.
    ///
    /// This is a (much) simplified version of the system described in the following paper:
    /// https://www.usenix.org/system/files/conference/fast16/fast16-papers-deligiannis.pdf
    ///
    /// The liveness bug (discussed in the paper) is injected in:
    ///     NodeManager.cs, line 181
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

        [Microsoft.PSharp.Test]
        public static void Execute(IMachineRuntime runtime)
        {
            runtime.RegisterMonitor(typeof(LivenessMonitor));
            runtime.CreateMachine(typeof(Environment));
        }
    }
}
