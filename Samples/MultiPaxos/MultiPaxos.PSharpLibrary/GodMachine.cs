// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace MultiPaxos.PSharpLibrary
{
    internal class GodMachine : Machine
    {
        List<MachineId> PaxosNodes;
        MachineId Client;

		[Start]
        [OnEntry(nameof(InitOnEntry))]
        class Init : MachineState { }

		void InitOnEntry()
        {
            this.PaxosNodes = new List<MachineId>();

            this.PaxosNodes.Insert(0, this.CreateMachine(typeof(PaxosNode)));
            this.Send(this.PaxosNodes[0], new PaxosNode.Config(3));

            this.PaxosNodes.Insert(0, this.CreateMachine(typeof(PaxosNode)));
            this.Send(this.PaxosNodes[0], new PaxosNode.Config(2));

            this.PaxosNodes.Insert(0, this.CreateMachine(typeof(PaxosNode)));
            this.Send(this.PaxosNodes[0], new PaxosNode.Config(1));

            foreach (var node in this.PaxosNodes)
            {
                this.Send(node, new PaxosNode.AllNodes(this.PaxosNodes));
            }

            this.Client = this.CreateMachine(typeof(Client));
            this.Send(this.Client, new Client.Config(this.PaxosNodes));
        }
    }
}
