﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

using Xunit;

namespace Microsoft.PSharp.SharedObjects.Tests.Unit
{
    public class SharedRegisterStructMockTest : BaseTest
    {
        struct S
        {
            public int f;
            public int g;

            public S(int f, int g)
            {
                this.f = f;
                this.g = g;
            }
        };

        class E : Event
        {
            public ISharedRegister<S> counter;

            public E(ISharedRegister<S> counter)
            {
                this.counter = counter;
            }
        }

        class Eflag : Event
        {
            public bool flag;

            public Eflag(bool flag)
            {
                this.flag = flag;
            }
        }

        class M : Machine
        {
            [Start]
            [OnEntry(nameof(EntryInit))]
            class Init : MachineState { }

            void EntryInit()
            {
                var flag = (this.ReceivedEvent as Eflag).flag;

                var counter = SharedRegister.Create<S>(this.Id.Runtime);
                counter.SetValue(new S(1, 1));

                this.CreateMachine(typeof(N), new E(counter));

                counter.Update(x =>
                {
                    return new S(x.f + 1, x.g + 1);
                });

                var v = counter.GetValue();

                this.Assert(v.f == v.g); // succeeds

                if (flag)
                {
                    this.Assert(v.f == 2 || v.f == 5 || v.f == 6); // succeeds
                }
                else
                {
                    this.Assert(v.f == 2 || v.f == 6); // fails
                }
            }
        }

        class N : Machine
        {
            [Start]
            [OnEntry(nameof(EntryInit))]
            class Init : MachineState { }

            void EntryInit()
            {
                var counter = (this.ReceivedEvent as E).counter;
                counter.SetValue(new S(5, 5));
            }
        }

        [Fact]
        public void TestRegisterStructSuccess()
        {
            var config = Configuration.Create().WithNumberOfIterations(100);

            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M), new Eflag(true));
            });

            base.AssertSucceeded(config, test);
        }

        [Fact]
        public void TestRegisterStructFail()
        {
            var config = Configuration.Create().WithNumberOfIterations(100);

            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M), new Eflag(false));
            });

            base.AssertFailed(config, test, "Detected an assertion failure.");
        }
    }
}
