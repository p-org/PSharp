﻿//-----------------------------------------------------------------------
// <copyright file="SEMOneMachine36Test.cs" company="Microsoft">
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
    public class SEMOneMachine36Test : BasePSharpTest
    {
        /// <summary>
        /// P# semantics test: one machine: "null" handler semantics.
        /// Testing that null handler is enabled in the simplest case.
        /// </summary>
        [TestMethod]
        public void TestNullEventHandler1()
        {
            var test = @"
using System;
using Microsoft.PSharp;

namespace SystematicTesting
{
    class E1 : Event {
        public E1() : base(1, -1) { }
    }

    class Unit : Event {
        public Unit() : base(1, -1) { }
    }

    class Real1 : Machine
    {
        bool test = false;

        [Start]
        [OnEntry(nameof(EntryInit))]
        [OnExit(nameof(ExitInit))]
        [OnEventGotoState(typeof(Default), typeof(S1))]
        [OnEventDoAction(typeof(Unit), nameof(InitAction))]
        [OnEventDoAction(typeof(E1), nameof(Action2))]
        class Init : MachineState { }

        void EntryInit()
        {
            this.Raise(new Unit());
        }

        void ExitInit() { }

        void InitAction()
        {
            this.Send(this.Id, new E1());
        }

        [OnEntry(nameof(EntryS1))]
        class S1 : MachineState { }

        void EntryS1()
        {
            this.Assert(test == false); // reachable
        }

        void Action2()
        {
            test = true;
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

            var parser = new CSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test), true);
            var program = parser.Parse();
            program.Rewrite();

            Configuration.SuppressTrace = true;
            Configuration.Verbose = 2;

            var assembly = base.GetAssembly(program.GetSyntaxTree());
            AnalysisContext.Create(assembly);

            SCTEngine.Setup();
            SCTEngine.Run();

            Assert.AreEqual(1, SCTEngine.NumOfFoundBugs);
        }
    }
}
