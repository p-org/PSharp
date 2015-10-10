//-----------------------------------------------------------------------
// <copyright file="SEMTwoMachines10Test.cs" company="Microsoft">
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
    public class SEMTwoMachines10Test : BasePSharpTest
    {
        [TestMethod]
        public void TestTwoMachines10()
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

    class Real1 : Machine
    {
        bool test = false;
        Id mac;

        [Start]
        [OnEntry(nameof(EntryInit))]
        [OnExit(nameof(ExitInit))]
        [OnEventGotoState(typeof(Default), typeof(S1))]
        [OnEventDoAction(typeof(E1), nameof(Action1))]
        class Init : MachineState { }

        void EntryInit()
        {
            mac = this.CreateMachine(typeof(Real2), this.Id);
            this.Raise(new E1());
        }

        void ExitInit()
        {
            this.Send(mac, new E2(), test);
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
            if (this.Trigger == typeof(E2))
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
            this.Assert((bool)this.Payload == false); // reachable
        }
    }

    class M : Monitor
    {
        [Start]
        [OnEntry(nameof(EntryX))]
        class X : MonitorState { }

        void EntryX()
        {
            //this.Assert((bool)this.Payload == true); // reachable
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
            PSharpRuntime.CreateMachine(typeof(Real1));
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
