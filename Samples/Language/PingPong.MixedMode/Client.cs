// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.PSharp;

namespace PingPong.MixedMode
{
    /// <summary>
    /// We use the partial keyword to declare the high-level state-machine
    /// transitions in the Client.psharp file, and the action-handler
    /// implementation in the Client.cs file.
    /// </summary>
    internal partial class Client : Machine
    {
        partial void SendPing()
        {
            this.Counter++;

            this.Send(this.Server, new Ping(this.Id));

            this.Logger.WriteLine("Client request: {0} / 5", this.Counter);

            if (this.Counter == 5)
            {
                this.Raise(new Halt());
            }
        }
    }
}
