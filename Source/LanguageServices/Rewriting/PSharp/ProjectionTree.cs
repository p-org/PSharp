// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Microsoft.PSharp.LanguageServices.Rewriting.PSharp
{
    /// <summary>
    /// Manages the collection of ProjectionNodes mapping to offsets in the rewritten C# file.
    /// </summary>
    /// <remarks>TODO: Currently stubbed out and expanded in a separate branch</remarks>
    public class ProjectionTree
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Temporary stub")]
        internal IReadOnlyList<ProjectionNode> OrderedCSharpProjectionNodes => Array.Empty<ProjectionNode>();

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", Justification = "Temporary stub")]
        internal ProjectionTree(IPSharpProgram program)
        {
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Temporary stub")]
        internal void OnInitialRewriteComplete()
        {
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Temporary stub")]
        internal void OnFinalRewriteComplete()
        {
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", Justification = "Temporary stub")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Temporary stub")]
        internal void AddRootChild(ProjectionNode child)
        {
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", Justification = "Temporary stub")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Temporary stub")]
        internal void FinalizeInitialOffsets(int offsetAdjustment, string rewrittenCSharpText)
        {
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", Justification = "Temporary stub")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Temporary stub")]
        internal void UpdateRewrittenCSharpText(string rewrittenCSharpText)
        {
        }
    }
}
