using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace PElevator
{
    public class Test
    {
        static void Main(string[] args)
        {
            Runtime.RegisterMachine(typeof(Elevator));
            Runtime.RegisterMachine(typeof(User));
            Runtime.RegisterMachine(typeof(Door));
            Runtime.RegisterMachine(typeof(Timer));

            Runtime.Start();
        }
    }
}
