//-----------------------------------------------------------------------
// <copyright file="Actions5FailTest.cs" company="Microsoft">
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
    /// <summary>
    /// Tests the semantics of push transitions and inheritance of actions.
    /// </summary>
    [TestClass]
    public class Actions5FailTest : BasePSharpTest
    {
        [TestMethod]
        public void TestActions5Fail()
        {
            var test = @"
using System;
using Microsoft.PSharp;

namespace SystematicTesting
{
    class E1 : Event {
        public E1() : base(1, -1) { }
    }

    class E2 : Event {
        public E2() : base(1, -1) { }
    }

    class E3 : Event {
        public E3() : base(1, -1) { }
    }

    class E4 : Event {
        public E4() : base(1, -1) { }
    }

    class Unit : Event {
        public Unit() : base(1, -1) { }
    }

    class Real : Machine
    {
        MachineId GhostMachine;

        [Start]
        [OnEntry(nameof(EntryInit))]
        [OnEventGotoState(typeof(E4), typeof(S2))]
        [OnEventPushState(typeof(Unit), typeof(S1))]
        [OnEventDoAction(typeof(E2), nameof(Action1))]
        class Init : MachineState { }

        void EntryInit()
        {
            GhostMachine = this.CreateMachine(typeof(Ghost), this.Id);
            this.Raise(new Unit());
        }

        [OnEntry(nameof(EntryS1))]
        class S1 : MachineState { }

        void EntryS1()
        {
            this.Send(GhostMachine, new E1());

			// we wait in this state until E2 comes from Ghost,
			// then handle E2 using the inherited handler Action1 
			// installed by Init
			// then wait until E4 comes from Ghost, and since
			// there's no handler for E4 in this pushed state, 
			// this state is popped, and E4 goto handler from Init 
			// is invoked
        }

        [OnEntry(nameof(EntryS2))]
        class S2 : MachineState { }

        void EntryS2()
        {
            // this assert is reachable
            this.Assert(false);
        }

        void Action1()
        {
            this.Send(GhostMachine, new E3());
        }
    }

    class Ghost : Machine
    {
        MachineId RealMachine;

        [Start]
        [OnEntry(nameof(EntryInit))]
        [OnEventGotoState(typeof(E1), typeof(S1))]
        class Init : MachineState { }

        void EntryInit()
        {
            RealMachine = this.Payload as MachineId;
        }

        [OnEntry(nameof(EntryS1))]
        [OnEventGotoState(typeof(E3), typeof(S2))]
        class S1 : MachineState { }

        void EntryS1()
        {
            this.Send(RealMachine, new E2());
        }

        [OnEntry(nameof(EntryS2))]
        class S2 : MachineState { }

        void EntryS2()
        {
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
            PSharpRuntime.CreateMachine(typeof(Real));
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
