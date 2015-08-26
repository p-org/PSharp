//-----------------------------------------------------------------------
// <copyright file="SEMOneMachine17Test.cs" company="Microsoft">
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
    public class SEMOneMachine17Test : BasePSharpTest
    {
        /// <summary>
        /// P# semantics test: one machine, "push" with implicit "pop" when the
        /// unhandled event was raised. This test checks implicit popping of the
        /// state when there's an unhandled event which was raised.
        /// </summary>
        [TestMethod]
        public void TestPushImplicitPopWithRaise()
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

    class Real1 : Machine
    {
        bool test = false;

        [Start]
        [OnEntry(nameof(EntryInit))]
        [OnExit(nameof(ExitInit))]
        [OnEventGotoState(typeof(E1), typeof(Init))]
        [OnEventPushState(typeof(E2), typeof(S1))]
        [OnEventDoAction(typeof(E3), nameof(Action1))]
        class Init : MachineState { }

        void EntryInit()
        {
            this.Send(this.Id, new E1());
        }

        void ExitInit()
        {
            this.Send(this.Id, new E2());
        }

        [OnEntry(nameof(EntryS1))]
        class S1 : MachineState { }

        void EntryS1()
        {
            test = true;
            this.Raise(new E1());
        }

        void Action1() { }
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
            PSharpRuntime.CreateMachine(typeof(Real1));
        }
    }
}";

            var parserConfig = new LanguageServicesConfiguration();
            var parser = new CSharpParser(new PSharpProject(parserConfig),
                SyntaxFactory.ParseSyntaxTree(test), true);
            var program = parser.Parse();
            program.Rewrite();

            var sctConfig = new DynamicAnalysisConfiguration();
            sctConfig.SuppressTrace = true;
            sctConfig.Verbose = 2;

            var assembly = base.GetAssembly(program.GetSyntaxTree());
            var context = AnalysisContext.Create(sctConfig, assembly);
            var sctEngine = SCTEngine.Create(context).Run();

            Assert.AreEqual(1, sctEngine.NumOfFoundBugs);
        }
    }
}
