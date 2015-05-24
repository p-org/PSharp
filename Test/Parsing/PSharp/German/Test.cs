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
            Runtime.RegisterNewEvent(typeof(unit));
            Runtime.RegisterNewEvent(typeof(req_share));
            Runtime.RegisterNewEvent(typeof(req_excl));
            Runtime.RegisterNewEvent(typeof(need_invalidate));
            Runtime.RegisterNewEvent(typeof(invalidate_ack));
            Runtime.RegisterNewEvent(typeof(grant));
            Runtime.RegisterNewEvent(typeof(ask_share));
            Runtime.RegisterNewEvent(typeof(ask_excl));
            Runtime.RegisterNewEvent(typeof(invalidate));
            Runtime.RegisterNewEvent(typeof(grant_excl));
            Runtime.RegisterNewEvent(typeof(grant_share));
            Runtime.RegisterNewEvent(typeof(normal));
            Runtime.RegisterNewEvent(typeof(wait));
            Runtime.RegisterNewEvent(typeof(invalidate_sharers));
            Runtime.RegisterNewEvent(typeof(sharer_id));

            Runtime.RegisterNewMachine(typeof(Host));
            Runtime.RegisterNewMachine(typeof(Client));
            Runtime.RegisterNewMachine(typeof(CPU));

            Runtime.Start();
        }
    }
}
