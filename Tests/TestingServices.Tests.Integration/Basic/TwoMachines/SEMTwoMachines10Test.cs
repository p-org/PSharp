﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

using Microsoft.PSharp.Utilities;

using Xunit;

namespace Microsoft.PSharp.TestingServices.Tests.Integration
{
    public class SEMTwoMachines10Test : BaseTest
    {
        class E1 : Event
        {
            public E1() : base(1, -1) { }
        }

        class E2 : Event
        {
            public bool Value;
            public E2(bool value) : base(1, -1) { this.Value = value; }
        }

        class Real1 : Machine
        {
            bool test = false;
            MachineId mac;

            [Start]
            [OnEntry(nameof(EntryInit))]
            [OnExit(nameof(ExitInit))]
            [OnEventGotoState(typeof(Default), typeof(S1))]
            [OnEventDoAction(typeof(E1), nameof(Action1))]
            class Init : MachineState { }

            void EntryInit()
            {
                mac = this.CreateMachine(typeof(Real2));
                this.Raise(new E1());
            }

            void ExitInit()
            {
                this.Send(mac, new E2(test));
            }

            class S1 : MachineState { }

            void Action1()
            {
                test = true;
            }
        }

        class Real2 : Machine
        {
            [Start]
            [OnEntry(nameof(EntryInit))]
            [OnEventDoAction(typeof(E2), nameof(EntryAction))]
            class Init : MachineState { }

            void EntryInit() { }

            void EntryAction()
            {
                if (this.ReceivedEvent.GetType() == typeof(E2))
                {
                    Action2();
                }
                else
                {
                    //this.Assert(false); // unreachable
                }
            }

            void Action2()
            {
                this.Assert((this.ReceivedEvent as E2).Value == false); // reachable
            }
        }

        class M : Monitor
        {
            [Start]
            [OnEntry(nameof(EntryX))]
            class X : MonitorState { }

            void EntryX()
            {
                //this.Assert((this.ReceivedEvent as E2).Value == true); // reachable
            }
        }

        [Fact]
        public void TestTwoMachines10()
        {
            var configuration = base.GetConfiguration();
            configuration.SchedulingStrategy = SchedulingStrategy.DFS;

            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(Real1));
            });

            base.AssertFailed(configuration, test, 1);
        }
    }
}
