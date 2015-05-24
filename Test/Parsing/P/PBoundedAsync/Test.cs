using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace PBoundedAsync
{
    public class Test
    {
        static void Main(string[] args)
        {
            Test.Execute();
        }

        [EntryPoint]
        public static void Execute()
        {
            Runtime.RegisterNewEvent(typeof(unit));
            Runtime.RegisterNewEvent(typeof(Req));
            Runtime.RegisterNewEvent(typeof(Resp));
            Runtime.RegisterNewEvent(typeof(init));
            Runtime.RegisterNewEvent(typeof(myCount));

            Runtime.RegisterNewMachine(typeof(Scheduler));
            Runtime.RegisterNewMachine(typeof(Process));

            Runtime.Start();
        }
    }
}
