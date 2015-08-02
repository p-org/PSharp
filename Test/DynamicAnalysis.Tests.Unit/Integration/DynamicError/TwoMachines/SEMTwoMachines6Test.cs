﻿//-----------------------------------------------------------------------
// <copyright file="SEMTwoMachines6Test.cs" company="Microsoft">
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

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Microsoft.PSharp.LanguageServices;
using Microsoft.PSharp.LanguageServices.Parsing;
using Microsoft.PSharp.Tooling;

namespace Microsoft.PSharp.DynamicAnalysis.Tests.Unit
{
    [TestClass]
    public class SEMTwoMachines6Test : BasePSharpTest
    {
        /// <summary>
        ///  P# semantics test: two machines, machine is halted with "raise halt"
        /// (handled). This test is for the case when "halt" is explicitly handled
        /// - hence, it is processed as any other event.
        /// </summary>
        [TestMethod]
        public void TestRaisedHaltHandled()
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

    class PING : Machine
    {
        MachineId PongId;
        int Count1 = 0;

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
            this.Send(PongId, new Ping(), this.Id);
            this.Raise(new Success());
        }

        [OnEventGotoState(typeof(Pong), typeof(SendPing))]
        class WaitPong : MachineState { }

        class Done : MachineState { }
    }

    class PONG : Machine
    {
        int Count2 = 0;

        [Start]
        [OnEntry(nameof(EntryWaitPing))]
        [OnEventGotoState(typeof(Ping), typeof(SendPong))]
        class WaitPing : MachineState { }

        void EntryWaitPing() { }

        [OnEntry(nameof(EntrySendPong))]
        [OnEventGotoState(typeof(Success), typeof(WaitPing))]
        [OnEventDoAction(typeof(Halt), nameof(Action1))]
        class SendPong : MachineState { }

        void EntrySendPong()
        {
            Count2 = Count2 + 1;
            
            if (Count2 == 1)
            {
                this.Send(this.Payload as MachineId, new Pong());
            }

            if (Count2 == 2)
            {
                this.Send(this.Payload as MachineId, new Pong());
                this.Raise(new Halt());
            }

            this.Raise(new Success());
        }

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

            var parser = new CSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test), true);
            var program = parser.Parse();
            program.Rewrite();

            Configuration.SuppressTrace = true;
            Configuration.Verbose = 2;
            Configuration.SchedulingStrategy = "dfs";

            var assembly = base.GetAssembly(program.GetSyntaxTree());
            AnalysisContext.Create(assembly);

            SCTEngine.Setup();
            SCTEngine.Run();

            Assert.AreEqual(1, SCTEngine.NumOfFoundBugs);
        }
    }
}
