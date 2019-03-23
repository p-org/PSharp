// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Microsoft.PSharp;

namespace CacheCoherence
{
    /// <summary>
    /// A single-process implementation of the cache coherence protocol by Steven German
    /// written using the P# high-level syntax.
    ///
    /// An overview of the protocol is described in the following tutorial:
    /// http://www.cs.utah.edu/~ganesh/presentations/fmcad04_tutorial2/chou/ctchou-tutorial.pdf
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
            runtime.CreateMachine(typeof(Host));
        }
    }
}