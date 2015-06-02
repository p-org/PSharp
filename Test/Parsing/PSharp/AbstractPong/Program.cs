using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace AbstractPong
{
    public class Program
    {
        static void Main(string[] args)
        {
            Runtime.RegisterMachine(typeof(Server));

            Runtime.Start();
        }
    }
}
