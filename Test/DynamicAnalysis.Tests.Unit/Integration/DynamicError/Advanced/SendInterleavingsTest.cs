//-----------------------------------------------------------------------
// <copyright file="SendInterleavingsTest.cs" company="Microsoft">
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
    public class SendInterleavingsTest : BasePSharpTest
    {
        [TestMethod]
        public void TestSendInterleavingsAssertionFailure()
        {
            var test = @"
using System;
using Microsoft.PSharp;

namespace SystematicTesting
{
    class Event1 : Event { }
    class Event2 : Event { }

    class Receiver : Machine
    {
        [Start]
        [OnEntry(nameof(Initialize))]
        [OnEventDoAction(typeof(Event1), nameof(OnEvent1))]
        [OnEventDoAction(typeof(Event2), nameof(OnEvent2))]
        class Init : MachineState { }

        int count1 = 0;
        void Initialize()
        {
            CreateMachine(typeof(Sender1), this.Id);
            CreateMachine(typeof(Sender2), this.Id);
        }

        void OnEvent1()
        {
            count1++;
        }
        void OnEvent2()
        {
            Assert(count1 != 1);
        }
    }

    class Sender1 : Machine
    {
        [Start]
        [OnEntry(nameof(Run))]
        class State : MachineState { }

        void Run()
        {
            Send((MachineId)Payload, new Event1());
            Send((MachineId)Payload, new Event1());
        }
    }

    class Sender2 : Machine
    {
        [Start]
        [OnEntry(nameof(Run))]
        class State : MachineState { }

        void Run()
        {
            Send((MachineId)Payload, new Event2());
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
            PSharpRuntime.CreateMachine(typeof(Receiver));
        }
    }
}";

            var parser = new CSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test), true);
            var program = parser.Parse();
            program.Rewrite();

            Configuration.Verbose = 2;
            Configuration.SchedulingIterations = 19;
            Configuration.SchedulingStrategy = "dfs";

            var assembly = base.GetAssembly(program.GetSyntaxTree());
            AnalysisContext.Create(assembly);

            SCTEngine.Setup();
            SCTEngine.Run();

            Assert.AreEqual(1, SCTEngine.NumOfFoundBugs);
        }
    }
}
