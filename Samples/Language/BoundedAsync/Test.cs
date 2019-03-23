// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Microsoft.PSharp;

namespace BoundedAsync
{
    /// <summary>
    /// A sample P# application written using the P# high-level syntax.
    ///
    /// The P# runtime starts by creating the P# machine 'Scheduler'. The 'Scheduler' machine
    /// then creates a user-defined number of 'Process' machines, which communicate with each
    /// other by exchanging a 'count' value. The processes assert that their count value is
    /// always equal (or minus one) to their neighbour's count value.
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
            runtime.CreateMachine(typeof(Scheduler), new Scheduler.Config(3));
        }
    }
}
