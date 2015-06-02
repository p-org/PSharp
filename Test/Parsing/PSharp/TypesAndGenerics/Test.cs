using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace TypesAndGenerics
{
    public class Test
    {
        static void Main(string[] args)
        {
            Runtime.RegisterMachine(typeof(Server));
            Runtime.RegisterMachine(typeof(Client));
            
            Runtime.Start();
        }
    }
}
