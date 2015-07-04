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
using System.IO;
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Microsoft.PSharp.LanguageServices;
using Microsoft.PSharp.LanguageServices.Parsing;
using Microsoft.PSharp.Tooling;

namespace Microsoft.PSharp.DynamicAnalysis.Tests.Unit
{
    [TestClass]
    public class SendInterleavingsFailTest
    {
        #region tests

        [TestMethod]
        public void TestSendInterleavingsAssertionFailure()
        {
            var test = @"
using System;
using Microsoft.PSharp;

namespace PSharpSendInterleavingTest
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

        [EntryPoint]
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

            var assembly = this.GetAssembly(program.GetSyntaxTree());
            AnalysisContext.Create(assembly);

            SCTEngine.Setup();
            SCTEngine.Run();

            Assert.AreEqual(1, SCTEngine.NumOfFoundBugs);
        }

        #endregion

        #region helper methods

        /// <summary>
        /// Get assembly from the given text.
        /// </summary>
        /// <param name="tree">SyntaxTree</param>
        /// <returns>Assembly</returns>
        private Assembly GetAssembly(SyntaxTree tree)
        {
            Assembly assembly = null;
            
            var references = new MetadataReference[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Machine).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(BugFindingDispatcher).Assembly.Location)
            };

            var compilation = CSharpCompilation.Create(
                "PSharpTestAssembly", new[] { tree }, references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            using (var ms = new MemoryStream())
            {
                var result = compilation.Emit(ms);
                if (result.Success)
                {
                    ms.Seek(0, SeekOrigin.Begin);
                    assembly = Assembly.Load(ms.ToArray());
                }
            }

            return assembly;
        }

        #endregion
    }
}
