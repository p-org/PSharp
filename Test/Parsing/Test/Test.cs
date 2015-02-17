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

    #region Machines

    [Main]
    internal machine Server
    {
        private Client Client;

        [Initial]
        private state Init
        {
            entry
            {
                this.Client = create Client { this };
                raise Unit;
            }

            on Unit goto Playing;
        }

        private state Playing
        {
            entry
            {
                send Pong to this.Client;
            }

            on Unit do SendPong;
            on Ping do SendPong;
            on Stop do StopIt;
        }

        private action SendPong
        {
            send Pong to this.Client;
        }

        private action StopIt
        {
            Console.WriteLine("Server stopped.\n");
            delete;
        }
    }

    internal machine Client
    {
        private Machine Server;
        private int Counter;

        [Initial]
        private state Init
        {
            entry
            {
                this.Server = (Machine) this.Payload;
                this.Counter = 0;
                raise Unit;
            }

            on Unit goto Playing;
        }

        private state Playing
        {
            entry
            {
                if (this.Counter == 5)
                {
                    send Stop to this.Server;
                    this.StopIt();
                }
            }

            on Unit goto Playing;
            on Pong do SendPing;
            on Stop do StopIt;
        }

        private action SendPing
        {
            this.Counter++;
            Console.WriteLine("\nTurns: {0} / 5\n", this.Counter);
            send Ping to this.Server;
            raise Unit;
        }

        private action StopIt
        {
            Console.WriteLine("Client stopped.\n");
            delete;
        }
    }

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
