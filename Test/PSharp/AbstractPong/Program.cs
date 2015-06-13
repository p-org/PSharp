using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace AbstractPong
{
    public class Program
    {
        static void Main(string[] args)
        {
            Program.Execute();
            Console.ReadLine();
        }

        [EntryPoint]
        public static void Execute()
        {
            Runtime.CreateMachine(typeof(Server));
        }
    }
}
