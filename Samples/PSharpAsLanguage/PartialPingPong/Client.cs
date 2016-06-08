using System;
using Microsoft.PSharp;

namespace PartialPingPong
{
    internal partial class Client : Machine
    {
        partial void SendPing()
        {
            this.Counter++;
            Console.WriteLine("\nTurns: {0} / 5\n", this.Counter);
            this.Send(this.Server, new Ping());
            this.Raise(new Unit());
        }
    }
}
