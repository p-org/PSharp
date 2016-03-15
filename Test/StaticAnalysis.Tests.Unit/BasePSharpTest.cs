//-----------------------------------------------------------------------
// <copyright file="BasePSharpTest.cs" company="Microsoft">
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

using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.PSharp.StaticAnalysis.Tests.Unit
{
    public abstract class BasePSharpTest
    {
        /// <summary>
        /// Get solution from the given text.
        /// </summary>
        /// <param name="text">Text</param>
        /// <returns>Solution</returns>
        protected Solution GetSolution(string text)
        {
            var workspace = new AdhocWorkspace();
            var solutionInfo = SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Create());
            var solution = workspace.AddSolution(solutionInfo);
            var project = workspace.AddProject("Test", "C#");

            var references = new MetadataReference[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Machine).Assembly.Location)
            };

            project = project.AddMetadataReferences(references);
            workspace.TryApplyChanges(project.Solution);

            var sourceText = SourceText.From(text);
            var doc = project.AddDocument("Program", sourceText, null, "Program.cs");

            return doc.Project.Solution;
        }
    }
}
