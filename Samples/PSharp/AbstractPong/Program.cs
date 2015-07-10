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

        [Microsoft.PSharp.Test]
        public static void Execute()
        {
            PSharpRuntime.CreateMachine(typeof(Server));
        }
    }
}
