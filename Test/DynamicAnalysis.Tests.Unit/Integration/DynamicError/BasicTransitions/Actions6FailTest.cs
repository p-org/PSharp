//-----------------------------------------------------------------------
// <copyright file="Actions6FailTest.cs" company="Microsoft">
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
    /// <summary>
    /// Tests that payload works correctly with a push transition.
    /// </summary>
    [TestClass]
    public class Actions6FailTest : BasePSharpTest
    {
        [TestMethod]
        public void TestActions6Fail()
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
            this.Assert((int)this.Payload == 100); // this assert passes
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
            this.Send(RealMachine, new E2(), 100);
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

            var parser = new CSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test), true);
            var program = parser.Parse();
            program.Rewrite();

            Configuration.ExportTrace = false;
            Configuration.Verbose = 2;
            Configuration.SchedulingStrategy = "dfs";

            var assembly = base.GetAssembly(program.GetSyntaxTree());
            AnalysisContext.Create(assembly);

            SCTEngine.Setup();
            SCTEngine.Run();

            Assert.AreEqual(1, SCTEngine.NumOfFoundBugs);
            Assert.AreEqual(5, SCTEngine.ExploredDepth);
        }
    }
}
