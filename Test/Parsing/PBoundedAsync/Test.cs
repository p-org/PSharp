using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace PBoundedAsync
{
    public class Test
    {
        static void Main(string[] args)
        {
            Runtime.RegisterNewEvent(typeof(unit));
            Runtime.RegisterNewEvent(typeof(Req));
            Runtime.RegisterNewEvent(typeof(Resp));
            Runtime.RegisterNewEvent(typeof(init));
            Runtime.RegisterNewEvent(typeof(myCount));

            Runtime.RegisterNewMachine(typeof(Scheduler));
            Runtime.RegisterNewMachine(typeof(Process));
            Runtime.Options.Verbose = true;

            Runtime.Start();
            Runtime.Wait();
            Runtime.Dispose();
        }
    }
}
