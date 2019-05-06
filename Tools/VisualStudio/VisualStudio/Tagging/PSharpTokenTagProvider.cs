// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.PSharp.VisualStudio
{
    /// <summary>
    /// The P# token tag provider.
    /// </summary>
    [Export(typeof(ITaggerProvider))]
    [ContentType("psharp")]
    [TagType(typeof(PSharpTokenTag))]
    internal sealed class PSharpTokenTagProvider : ITaggerProvider
    {
        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
            => new PSharpTokenTagger(buffer) as ITagger<T>;
    }
}
