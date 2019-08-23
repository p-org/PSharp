using System;
using System.Collections.Generic;

namespace Microsoft.PSharp.LanguageServices.Rewriting.PSharp
{
    /// <summary>
    /// Manages the collection of projection nodes mapping to offsets in the rewritten C# file.
    /// </summary>
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
