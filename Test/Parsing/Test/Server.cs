using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace ParsingTest
{
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
}
