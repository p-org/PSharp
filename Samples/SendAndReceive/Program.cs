// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp;

namespace SendAndReceive
{
    class Program
    {
        static void Main(string[] args)
        {
            var runtime = PSharpRuntime.Create();

            // Create a machine.
            var mid = runtime.CreateMachine(typeof(M1));

            // Do some work.
            runtime.SendEvent(mid, new M1.Inc());
            runtime.SendEvent(mid, new M1.Inc());
            runtime.SendEvent(mid, new M1.Inc());

            // Grab the result from the machine.
            GetDataAndPrint(runtime, mid).Wait();
        }

        /// <summary>
        /// Gets result from the given machine.
        /// </summary>
        /// <param name="runtime">The P# runtime.</param>
        /// <param name="mid">Machine to get response from.</param>
        static async Task GetDataAndPrint(PSharpRuntime runtime, MachineId mid)
        {
            var resp = await GetReponseMachine<M1.Response>.GetResponse(runtime, mid, m => new M1.Get(m));
            Console.WriteLine("Got response: {0}", resp.v);
        }
    }

    /// <summary>
    /// A simple machine.
    /// </summary>
    class M1 : Machine
    {
        public class Get : Event
        {
            public MachineId Mid;
            public Get(MachineId mid)
            {
                this.Mid = mid;
            }
        }
        public class Inc : Event { }
        public class Response : Event
        {
            public int v;
            public Response(int v)
            {
                this.v = v;
            }
        }

        /// <summary>
        /// The counter.
        /// </summary>
        int x;

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventDoAction(typeof(Inc), nameof(DoInc))]
        [OnEventDoAction(typeof(Get), nameof(DoGet))]
        class Init : MachineState { }

        void InitOnEntry()
        {
            x = 0;
        }

        void DoInc()
        {
            x++;
        }

        /// <summary>
        /// Sends the current value of the counter.
        /// </summary>
        void DoGet()
        {
            var sender = (this.ReceivedEvent as Get).Mid;
            this.Send(sender, new Response(x));
        }
    }
}
