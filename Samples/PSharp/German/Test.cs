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
            Test.Execute();
            Console.ReadLine();
        }

        [Microsoft.PSharp.Test]
        public static void Execute()
        {
            PSharpRuntime.CreateMachine(typeof(Host));
        }
    }
}
