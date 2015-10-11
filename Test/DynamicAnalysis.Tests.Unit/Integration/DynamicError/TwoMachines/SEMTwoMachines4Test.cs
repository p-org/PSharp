//-----------------------------------------------------------------------
// <copyright file="SEMTwoMachines4Test.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation. All rights reserved.
// 
//      THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, 
//      EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES 
//      OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
// ----------------------------------------------------------------------------------
//      The example companies, organizations, products, domain names,
//      e-mail addresses, logos, people, places, and events depicted
//      herein are fictitious.  No association with any real company,
//      organization, product, domain name, email address, logo, person,
//      places, or events is intended or should be inferred.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Microsoft.PSharp.LanguageServices;
using Microsoft.PSharp.LanguageServices.Parsing;
using Microsoft.PSharp.Tooling;

namespace Microsoft.PSharp.DynamicAnalysis.Tests.Unit
{
    [TestClass]
    public class SEMTwoMachines4Test : BasePSharpTest
    {
        /// <summary>
        /// Tests that an event sent to a machine after it received the
        /// "halt" event is ignored by the halted machine.
        /// Case when "halt" is explicitly handled.
        /// </summary>
        [TestMethod]
        public void TestEventSentAfterSentHaltHandled()
        {
            var test = @"
using System;
using Microsoft.PSharp;

namespace SystematicTesting
{
    class Ping : Event {
        public Ping() : base(1, -1) { }
    }

    class Pong : Event {
        public Pong() : base(1, -1) { }
    }

    class Success : Event { }
    class PingIgnored : Event { }

    class PING : Machine
    {
        MachineId PongId;
        int Count;

        [Start]
        [OnEntry(nameof(EntryInit))]
        [OnEventGotoState(typeof(Success), typeof(SendPing))]
        class Init : MachineState { }

        void EntryInit()
        {
            PongId = this.CreateMachine(typeof(PONG));
            this.Raise(new Success());
        }

        [OnEntry(nameof(EntrySendPing))]
        [OnEventGotoState(typeof(Success), typeof(WaitPong))]
        class SendPing : MachineState { }

        void EntrySendPing()
        {
            Count = Count + 1;
            if (Count == 1)
            {
                this.Send(PongId, new Ping(), this.Id);
            }
            // halt PONG after one exchange
            if (Count == 2)
            {
                this.Send(PongId, new Halt());
                this.Send(PongId, new PingIgnored());
            }

            this.Raise(new Success());
        }

        [OnEventGotoState(typeof(Pong), typeof(SendPing))]
        class WaitPong : MachineState { }

        class Done : MachineState { }
    }

    class PONG : Machine
    {
        [Start]
        [OnEventGotoState(typeof(Ping), typeof(SendPong))]
        [OnEventGotoState(typeof(Halt), typeof(PongHalt))]
        class WaitPing : MachineState { }

        [OnEntry(nameof(EntrySendPong))]
        [OnEventGotoState(typeof(Success), typeof(WaitPing))]
        class SendPong : MachineState { }

        void EntrySendPong()
        {
            this.Send(this.Payload as MachineId, new Pong());
            this.Raise(new Success());
        }

        [OnEventDoAction(typeof(PingIgnored), nameof(Action1))]
        [IgnoreEvents(typeof(Ping))]
        class PongHalt : MachineState { }

        void Action1()
        {
            this.Assert(false); // reachable
        }
    }

    public static class TestProgram
    {
        public static void Main(string[] args)
        {
            TestProgram.Execute();
            Console.ReadLine();
        }

        [Test]
        public static void Execute()
        {
            PSharpRuntime.CreateMachine(typeof(PING));
        }
    }
}";

            var parser = new CSharpParser(new PSharpProject(),
                SyntaxFactory.ParseSyntaxTree(test), true);
            var program = parser.Parse();
            program.Rewrite();

            var sctConfig = DynamicAnalysisConfiguration.Create();
            sctConfig.SuppressTrace = true;
            sctConfig.Verbose = 2;
            sctConfig.SchedulingStrategy = SchedulingStrategy.DFS;

            var assembly = base.GetAssembly(program.GetSyntaxTree());
            var context = AnalysisContext.Create(sctConfig, assembly);
            var sctEngine = SCTEngine.Create(context).Run();

            Assert.AreEqual(1, sctEngine.NumOfFoundBugs);
        }
    }
}
