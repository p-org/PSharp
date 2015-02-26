using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace PBoundedAsync
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
            Runtime.RegisterNewEvent(typeof(eUnit));
            Runtime.RegisterNewEvent(typeof(eReq));
            Runtime.RegisterNewEvent(typeof(eResp));
            Runtime.RegisterNewEvent(typeof(eDone));
            Runtime.RegisterNewEvent(typeof(eInit));
            Runtime.RegisterNewEvent(typeof(eMyCount));

            Runtime.RegisterNewMachine(typeof(Scheduler));
            Runtime.RegisterNewMachine(typeof(Process));

            Runtime.Start();
            Runtime.Wait();
            Runtime.Dispose();
        }
    }
}
