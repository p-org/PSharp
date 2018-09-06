﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

using Microsoft.PSharp.Utilities;

using Xunit;

namespace Microsoft.PSharp.TestingServices.Tests.Integration
{
    public class SEMOneMachine33Test : BaseTest
    {
        class Config : Event
        {
            public List<int> List;
            public int V;
            public Config(List<int> l, int v) : base(-1, -1) { this.List = l; this.V = v; }
        }

        class Unit : Event { }

        class SeqPayload : Event
        {
            public List<int> List;
            public SeqPayload(List<int> l) : base(-1, -1) { this.List = l; }
        }

        class Entry : Machine
        {
            List<int> l;
            MachineId mac;

            [Start]
            [OnEntry(nameof(EntryInit))]
            class Init : MachineState { }

            void EntryInit()
            {
                l = new List<int>();
                l.Insert(0, 12);
                l.Insert(0, 23);
                l.Insert(0, 12);
                l.Insert(0, 23);
                l.Insert(0, 12);
                l.Insert(0, 23);
                mac = this.CreateMachine(typeof(Tester));
                this.Send(mac, new Config(l, 1));
                this.Send(mac, new SeqPayload(l));
            }
        }

        class Tester : Machine
        {
            List<int> ii;
            List<int> rec;
            int i;

            [Start]
            [OnEntry(nameof(EntryInit))]
            [OnEventDoAction(typeof(Config), nameof(Configure))]
            [OnEventGotoState(typeof(SeqPayload), typeof(TestItNow))]
            class Init : MachineState { }

            void EntryInit()
            {
                ii = new List<int>();
                rec = new List<int>();
            }

            void Configure()
            {
                ii = (this.ReceivedEvent as Config).List;
                this.Assert(ii[0] == 23);
                this.Assert((this.ReceivedEvent as Config).V == 1);
            }

            [OnEntry(nameof(EntryTestItNow))]
            class TestItNow : MachineState { }

            void EntryTestItNow()
            {
                rec = (this.ReceivedEvent as SeqPayload).List;
                i = rec.Count - 1;
                while (i >= 0)
                {
                    this.Assert(rec[i] == ii[i]);
                    i = i - 1;
                }
            }
        }

        [Fact]
        public void TestSEMOneMachine33()
        {
            var configuration = base.GetConfiguration();
            configuration.SchedulingStrategy = SchedulingStrategy.DFS;
            configuration.SchedulingIterations = 5;

            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(Entry));
            });

            base.AssertSucceeded(configuration, test);
        }
    }
}
