using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace MultiPaxos
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
            Runtime.RegisterMachine(typeof(GodMachine));
            Runtime.RegisterMachine(typeof(PaxosNode));
            Runtime.RegisterMachine(typeof(LeaderElection));
            Runtime.RegisterMachine(typeof(Client));
            Runtime.RegisterMachine(typeof(Timer));

            Runtime.Start();
        }
    }
}
