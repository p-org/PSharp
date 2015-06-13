using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace PTwoPhaseCommit
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
            Runtime.RegisterMachine(typeof(Timer));
            Runtime.RegisterMachine(typeof(Replica));
            Runtime.RegisterMachine(typeof(Coordinator));
            Runtime.RegisterMachine(typeof(Client));
            Runtime.RegisterMachine(typeof(TwoPhaseCommit));

            Runtime.Start();
        }
    }
}
