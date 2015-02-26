using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace AbstractPong
{
    public class Program
    {
        static void Main(string[] args)
        {
            Runtime.RegisterNewMachine(typeof(Server));

            Runtime.Start();
            Runtime.Wait();
            Runtime.Dispose();
        }
    }
}
