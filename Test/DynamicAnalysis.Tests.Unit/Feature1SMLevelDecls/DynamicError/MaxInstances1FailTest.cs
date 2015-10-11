//-----------------------------------------------------------------------
// <copyright file="MaxInstances1FailTest.cs" company="Microsoft">
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
    public class MaxInstances1FailTest : BasePSharpTest
    {
        [TestMethod]
        public void TestMaxInstances1AssertionFailure()
        {
            var test = @"
using System;
using Microsoft.PSharp;

namespace SystematicTesting
{
    class Config : Event {
        public MachineId Id;
        public Config(MachineId id) : base(-1, -1) { this.Id = id; }
    }

    class E1 : Event {
        public E1() : base(1, -1) { }
    }

    class E2 : Event {
        public int Value;
        public E2(int value) : base(1, -1) { this.Value = value; }
    }
    class E3 : Event {
        public E3() : base(-1, -1) { }
    }

    class E4 : Event { }

    class Unit : Event {
        public Unit() : base(1, -1) { }
    }

    class RealMachine : Machine
    {
        MachineId GhostMachine;

        [Start]
        [OnEntry(nameof(EntryInit))]
        [OnEventPushState(typeof(Unit), typeof(S1))]
        [OnEventGotoState(typeof(E4), typeof(S2))]
        [OnEventDoAction(typeof(E2), nameof(Action1))]
        class Init : MachineState { }

        void EntryInit()
        {
            GhostMachine = this.CreateMachine(typeof(GhostMachine));
            this.Send(GhostMachine, new Config(this.Id));
            this.Raise(new Unit());
        }

        [OnEntry(nameof(EntryS1))]
        class S1 : MachineState { }

        void EntryS1()
        {
            this.Send(GhostMachine, new E1());
            this.Send(GhostMachine, new E1()); // error
        }

        [OnEntry(nameof(EntryS2))]
        [OnEventGotoState(typeof(Unit), typeof(S3))]
        class S2 : MachineState { }

        void EntryS2()
        {
            this.Raise(new Unit());
        }

        [OnEventGotoState(typeof(E4), typeof(S3))]
        class S3 : MachineState { }

        void Action1()
        {
            this.Assert((this.ReceivedEvent as E2).Value == 100);
            this.Send(GhostMachine, new E3());
            this.Send(GhostMachine, new E3());
        }
    }

    class GhostMachine : Machine
    {
        MachineId RealMachine;

        [Start]
        [OnEventDoAction(typeof(Config), nameof(Configure))]
        [OnEventGotoState(typeof(Unit), typeof(GhostInit))]
        class Init : MachineState { }

        void Configure()
        {
            RealMachine = (this.ReceivedEvent as Config).Id;
            this.Raise(new Unit());
        }

        [OnEventGotoState(typeof(E1), typeof(S1))]
        class GhostInit : MachineState { }

        [OnEntry(nameof(EntryS1))]
        [OnEventGotoState(typeof(E3), typeof(S2))]
        [IgnoreEvents(typeof(E1))]
        class S1 : MachineState { }

        void EntryS1()
        {
            this.Send(RealMachine, new E2(100));
        }

        [OnEntry(nameof(EntryS2))]
        [OnEventGotoState(typeof(E3), typeof(GhostInit))]
        class S2 : MachineState { }

        void EntryS2()
        {
            this.Send(RealMachine, new E4());
            this.Send(RealMachine, new E4());
            this.Send(RealMachine, new E4());
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
            PSharpRuntime.CreateMachine(typeof(RealMachine));
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
            sctConfig.DepthBound = 3;

            var assembly = base.GetAssembly(program.GetSyntaxTree());
            var context = AnalysisContext.Create(sctConfig, assembly);
            var sctEngine = SCTEngine.Create(context).Run();

            Assert.AreEqual(1, sctEngine.NumOfFoundBugs);
        }
    }
}
