//-----------------------------------------------------------------------
// <copyright file="MethodCallTest.cs" company="Microsoft">
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
    public class MethodCallTest
    {
        #region tests

        [TestMethod]
        public void TestMethodCall()
        {
            var test = @"
using System;
using Microsoft.PSharp;

namespace SystematicTesting
{
    class E : Event { }

    class Program : Machine
    {
        int x;

        [Start]
        [OnEntry(nameof(EntryInit))]
        class Init : MachineState { }

        void EntryInit()
        {
            this.Foo(1, 3, x);
        }

        int Foo(int x, int y, int z)
        {
            return 0;
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
            PSharpRuntime.CreateMachine(typeof(Program));
        }
    }
}";

            var parser = new CSharpParser(new PSharpProject(), SyntaxFactory.ParseSyntaxTree(test), true);
            var program = parser.Parse();
            program.Rewrite();

            Configuration.Verbose = 2;

            var assembly = this.GetAssembly(program.GetSyntaxTree());
            AnalysisContext.Create(assembly);

            SCTEngine.Setup();
            SCTEngine.Run();

            Assert.AreEqual(0, SCTEngine.NumOfFoundBugs);
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
