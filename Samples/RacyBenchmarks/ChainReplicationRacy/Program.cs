using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace ChainReplicationRacy
{
    /// <summary>
    /// This is an example of usign P#.
    /// 
    /// This example implements the Chain Replication protocol
    /// from OSDI'04.
    /// </summary>
    public class Program
    {
        public static void Main(string[] args)
        {
            PSharpRuntime runtime = PSharpRuntime.Create();
            runtime.CreateMachine(typeof(GodMachine));
            Console.WriteLine("Done");
            Console.WriteLine("Press enter to exit");
            Console.ReadLine();
        }
    }
}
