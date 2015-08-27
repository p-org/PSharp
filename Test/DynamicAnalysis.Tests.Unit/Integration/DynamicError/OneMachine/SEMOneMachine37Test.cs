//-----------------------------------------------------------------------
// <copyright file="SEMOneMachine37Test.cs" company="Microsoft">
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
    public class SEMOneMachine37Test : BasePSharpTest
    {
        /// <summary>
        /// P# semantics test: one machine: "null" handler semantics.
        /// Testing that null handler is enabled in the simplest case.
        /// </summary>
        [TestMethod]
        public void TestNullEventHandler2()
        {
            var test = @"
using System;
using Microsoft.PSharp;

namespace SystematicTesting
{
    class Real1 : Machine
    {
        bool test = false;

        [Start]
        [OnEntry(nameof(EntryInit))]
        [OnExit(nameof(ExitInit))]
        [OnEventGotoState(typeof(Default), typeof(S1))]
        class Init : MachineState { }

        void EntryInit() { }

        void ExitInit() { }

        [OnEntry(nameof(EntryS1))]
        class S1 : MachineState { }

        void EntryS1()
        {
            this.Assert(test == true); // reachable
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
