﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests
{
    /// <summary>
    /// A single-process implementation of the dining philosophers problem.
    /// </summary>
    public class DiningPhilosophersTest : BaseTest
    {
        public DiningPhilosophersTest(ITestOutputHelper output)
            : base(output)
        { }

        class Environment : Machine
        {
            Dictionary<int, MachineId> LockMachines;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                LockMachines = new Dictionary<int, MachineId>();

                int n = 3;
                for (int i = 0; i < n; i++)
                {
                    var lck = CreateMachine(typeof(Lock));
                    LockMachines.Add(i, lck);
                }

                for (int i = 0; i < n; i++)
                {
                    CreateMachine(typeof(Philosopher), new Philosopher.Config(LockMachines[i], LockMachines[(i + 1) % n]));
                }
            }
        }

        class Lock : Machine
        {
            public class TryLock : Event
            {
                public MachineId Target;

                public TryLock(MachineId target)
                {
                    Target = target;
                }
            }

            public class Release : Event { }

            public class LockResp : Event
            {
                public bool LockResult;

                public LockResp(bool res)
                {
                    LockResult = res;
                }
            }

            bool LockVar;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            [OnEventDoAction(typeof(TryLock), nameof(OnTryLock))]
            [OnEventDoAction(typeof(Release), nameof(OnRelease))]
            class Waiting : MachineState { }

            void InitOnEntry()
            {
                LockVar = false;
                this.Goto<Waiting>();
            }

            void OnTryLock()
            {
                var target = (ReceivedEvent as TryLock).Target;
                if (LockVar)
                {
                    Send(target, new LockResp(false));
                }
                else
                {
                    LockVar = true;
                    Send(target, new LockResp(true));
                }
            }

            void OnRelease()
            {
                LockVar = false;
            }
        }

        class Philosopher : Machine
        {
            public class Config : Event
            {
                public MachineId Left;
                public MachineId Right;

                public Config(MachineId left, MachineId right)
                {
                    Left = left;
                    Right = right;
                }
            }

            class TryAgain : Event { }

            MachineId left;
            MachineId right;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            [OnEntry(nameof(TryAccess))]
            [OnEventDoAction(typeof(TryAgain), nameof(TryAccess))]
            class Trying : MachineState { }

            [OnEntry(nameof(OnDone))]
            class Done : MachineState { }

            void InitOnEntry()
            {
                var e = ReceivedEvent as Config;
                left = e.Left;
                right = e.Right;
                this.Goto<Trying>();
            }

            void TryAccess()
            {
                Send(left, new Lock.TryLock(Id));
                var ev = Receive(typeof(Lock.LockResp)).Result;
                if ((ev as Lock.LockResp).LockResult)
                {
                    Send(right, new Lock.TryLock(Id));
                    var evr = Receive(typeof(Lock.LockResp)).Result;
                    if ((evr as Lock.LockResp).LockResult)
                    {
                        this.Goto<Done>();
                        return;
                    }
                    else
                    {
                        Send(left, new Lock.Release());
                    }
                }

                Send(Id, new TryAgain());
            }

            void OnDone()
            {
                Send(left, new Lock.Release());
                Send(right, new Lock.Release());
                Monitor<LivenessMonitor>(new LivenessMonitor.NotifyDone());
                Raise(new Halt());
            }
        }

        class LivenessMonitor : Monitor
        {
            public class NotifyDone : Event { }

            [Start]
            [Hot]
            [OnEventGotoState(typeof(NotifyDone), typeof(Done))]
            class Init : MonitorState { }

            [Cold]
            [OnEventGotoState(typeof(NotifyDone), typeof(Done))]
            class Done : MonitorState { }
        }

        [Theory]
        [InlineData(52)]
        public void TestDiningPhilosophersLivenessBugWithCycleReplay(int seed)
        {
            var configuration = GetConfiguration();
            configuration.EnableCycleDetection = true;
            configuration.MaxUnfairSchedulingSteps = 100;
            configuration.MaxFairSchedulingSteps = 1000;
            configuration.LivenessTemperatureThreshold = 500;
            configuration.RandomSchedulingSeed = seed;
            configuration.SchedulingIterations = 1;

            var test = new Action<PSharpRuntime>((r) => {
                r.RegisterMonitor(typeof(LivenessMonitor));
                r.CreateMachine(typeof(Environment));
            });

            var bugReport = "Monitor 'LivenessMonitor' detected infinite execution that violates a liveness property.";
            AssertFailed(configuration, test, bugReport, true);
        }
    }
}
