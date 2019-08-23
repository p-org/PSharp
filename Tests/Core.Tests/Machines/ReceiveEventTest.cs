﻿// ------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.PSharp.Runtime;
using Microsoft.PSharp.TestingServices;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.Core.Tests
{
    public class ReceiveEventTest : BaseTest
    {
        public ReceiveEventTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class E1 : Event
        {
        }

        private class E2 : Event
        {
        }

        private class E3 : Event
        {
        }

        private class M1 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private async Task InitOnEntry()
            {
                await this.Receive(typeof(E1));
            }
        }

        private class M2 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private async Task InitOnEntry()
            {
                await this.Receive(typeof(E1));
                await this.Receive(typeof(E2));
                await this.Receive(typeof(E3));
            }
        }

        private class M3 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private async Task InitOnEntry()
            {
                await this.Receive(typeof(E1), typeof(E2), typeof(E3));
            }
        }

        private class M4 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private async Task InitOnEntry()
            {
                await this.Receive(typeof(E1), typeof(E2), typeof(E3));
                await this.Receive(typeof(E1), typeof(E2), typeof(E3));
                await this.Receive(typeof(E1), typeof(E2), typeof(E3));
            }
        }

        [Fact(Timeout = 5000)]
        public async Task TestReceiveEventStatement()
        {
            var configuration = GetConfiguration();
            var test = new MachineTestKit<M1>(configuration: configuration);

            await test.StartMachineAsync();
            test.AssertIsWaitingToReceiveEvent(true);

            await test.SendEventAsync(new E1());
            test.AssertIsWaitingToReceiveEvent(false);
            test.AssertInboxSize(0);
        }

        [Fact(Timeout = 5000)]
        public async Task TestMultipleReceiveEventStatements()
        {
            var configuration = GetConfiguration();
            var test = new MachineTestKit<M2>(configuration: configuration);

            await test.StartMachineAsync();
            test.AssertIsWaitingToReceiveEvent(true);

            await test.SendEventAsync(new E1());
            test.AssertIsWaitingToReceiveEvent(true);

            await test.SendEventAsync(new E2());
            test.AssertIsWaitingToReceiveEvent(true);

            await test.SendEventAsync(new E3());
            test.AssertIsWaitingToReceiveEvent(false);
            test.AssertInboxSize(0);
        }

        [Fact(Timeout = 5000)]
        public async Task TestMultipleReceiveEventStatementsUnordered()
        {
            var configuration = GetConfiguration();
            var test = new MachineTestKit<M2>(configuration: configuration);

            await test.StartMachineAsync();
            test.AssertIsWaitingToReceiveEvent(true);

            await test.SendEventAsync(new E2());
            test.AssertIsWaitingToReceiveEvent(true);
            test.AssertInboxSize(1);

            await test.SendEventAsync(new E3());
            test.AssertIsWaitingToReceiveEvent(true);
            test.AssertInboxSize(2);

            await test.SendEventAsync(new E1());
            test.AssertIsWaitingToReceiveEvent(false);
            test.AssertInboxSize(0);
        }

        [Fact(Timeout = 5000)]
        public async Task TestReceiveEventStatementWithMultipleTypes()
        {
            var configuration = GetConfiguration();
            var test = new MachineTestKit<M3>(configuration: configuration);

            await test.StartMachineAsync();
            test.AssertIsWaitingToReceiveEvent(true);

            await test.SendEventAsync(new E1());
            test.AssertIsWaitingToReceiveEvent(false);
            test.AssertInboxSize(0);
        }

        [Fact(Timeout = 5000)]
        public async Task TestMultipleReceiveEventStatementsWithMultipleTypes()
        {
            var configuration = GetConfiguration();
            var test = new MachineTestKit<M4>(configuration: configuration);

            await test.StartMachineAsync();
            test.AssertIsWaitingToReceiveEvent(true);

            await test.SendEventAsync(new E1());
            test.AssertIsWaitingToReceiveEvent(true);

            await test.SendEventAsync(new E2());
            test.AssertIsWaitingToReceiveEvent(true);

            await test.SendEventAsync(new E3());
            test.AssertIsWaitingToReceiveEvent(false);
            test.AssertInboxSize(0);
        }
    }
}
