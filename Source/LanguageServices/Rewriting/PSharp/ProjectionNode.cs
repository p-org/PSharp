// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.PSharp.LanguageServices.Parsing;

namespace Microsoft.PSharp.LanguageServices.Rewriting.PSharp
{
    /// <summary>
    /// Offset and other information that will be used to create the VS Language Service
    /// Projection Buffers for the PSharp to CSharp rewritten form.
    /// </summary>
    /// <remarks>TODO: Currently stubbed out and expanded in a separate branch</remarks>
    public class ProjectionNode
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", Justification = "Temporary stub")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Temporary stub")]
        internal ProjectionNode(object node, ProjectionTree projectionTree = null)
        {
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", Justification = "Temporary stub")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Temporary stub")]
        internal void SetOffsetInParent(int offsetInParent)
        {
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", Justification = "Temporary stub")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Temporary stub")]
        internal void AddChild(ProjectionNode child, ProjectionNode csharpParent = null)
        {
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", Justification = "Temporary stub")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Temporary stub")]
        internal void SetHeaderInfo(TokenRange originalTokenRange, int rewrittenOffset, string rewrittenString)
        {
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", Justification = "Temporary stub")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Temporary stub")]
        internal void SetCodeChunkInfo(int originalStart, string originalString, int rewrittenOffset)
        {
        }
    }
}
