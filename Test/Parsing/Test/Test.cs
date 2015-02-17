using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace ParsingTest
{
    #region Events

    internal event Ping;
    internal event Pong;
    internal event Stop;
    internal event Unit;

    #endregion

    public class Test
    {
        static void Main(string[] args)
        {
            Runtime.RegisterNewEvent(typeof(Ping));
            Runtime.RegisterNewEvent(typeof(Pong));
            Runtime.RegisterNewEvent(typeof(Stop));
            Runtime.RegisterNewEvent(typeof(Unit));

            Runtime.RegisterNewMachine(typeof(Server));
            Runtime.RegisterNewMachine(typeof(Client));

            Runtime.Start();
            Runtime.Wait();
            Runtime.Dispose();
        }
    }
}
