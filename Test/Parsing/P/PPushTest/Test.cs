using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace PPushTest
{
    public class Test
    {
        static void Main(string[] args)
        {
            Runtime.RegisterMachine(typeof(Ping));
            Runtime.RegisterMachine(typeof(Pong));
            
            Runtime.Start();
        }
    }
}
