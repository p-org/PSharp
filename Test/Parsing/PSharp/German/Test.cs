using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace German
{
    #region C# Classes and Structs

    internal struct CountMessage
    {
        public int Count;

        public CountMessage(int count)
        {
            this.Count = count;
        }
    }

    #endregion

    public class Test
    {
        static void Main(string[] args)
        {
            Runtime.CreateMachine<Host>();
            Runtime.WaitMachines();
        }
    }
}
