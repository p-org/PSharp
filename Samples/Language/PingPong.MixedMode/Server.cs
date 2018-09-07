// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

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