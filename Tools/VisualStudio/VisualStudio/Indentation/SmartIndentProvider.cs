// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.PSharp.VisualStudio
{
    /// <summary>
    /// The P# smart indent provider.
    /// </summary>
    [Export(typeof(ISmartIndentProvider))]
    [ContentType("psharp")]
    internal sealed class SmartIndentProvider : ISmartIndentProvider
    {
        public ISmartIndent CreateSmartIndent(ITextView textView)
        {
            return new Indent(textView);
        }
    }
}
