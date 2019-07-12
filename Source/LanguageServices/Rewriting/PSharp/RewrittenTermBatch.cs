// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;

using Microsoft.CodeAnalysis;

namespace Microsoft.PSharp.LanguageServices.Rewriting.PSharp
{
    /// <summary>
    /// This class represents a list of instances of rewriting SyntaxNodes from P# terms to C# replacements.
    /// </summary>
    /// <remarks>TODO: Currently stubbed out and expanded in a separate branch</remarks>
    public class RewrittenTermBatch
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", Justification = "Temporary stub")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Temporary stub")]
        internal RewrittenTermBatch(IEnumerable<ProjectionNode> orderedCSharpProjectionNodes)
        {
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", Justification = "Temporary stub")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Temporary stub")]
        internal void AddToBatch(SyntaxNode node, string rewrittenText)
        {
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", Justification = "Temporary stub")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Temporary stub")]
        internal void OffsetStarts(int offset)
        {
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", Justification = "Temporary stub")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Temporary stub")]
        internal void MergeBatch()
        {
        }
    }
}
