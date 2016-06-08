using System;
using Microsoft.PSharp;

namespace PartialPingPong
{
    internal partial class Server : Machine
    {
        void SendPong()
        {
            this.Send(this.Client, new Pong());
        }
    }
}
