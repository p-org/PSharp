using Microsoft.PSharp;

namespace PingPong.MixedMode
{
    /// <summary>
    /// A P# machine that models a simple server.
    /// 
    /// It receives 'Ping' events from a client, and responds with a 'Pong' event.
    /// </summary>
    internal partial class Server : Machine
    {
        void SendPong()
        {
            var client = (this.ReceivedEvent as Client.Ping).client;
            this.Send(client, new Pong());
        }
    }
}