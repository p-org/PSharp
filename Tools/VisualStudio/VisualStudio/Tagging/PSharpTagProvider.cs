// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
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
    [TagType(typeof(IErrorTag))]
    internal sealed class PSharpTagProvider : ITaggerProvider
    {
        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            if (typeof(T) == typeof(PSharpTokenTag))
            {
                return buffer.Properties.GetOrCreateSingletonProperty(() => new PSharpTokenTagger(buffer) as ITagger<T>);
            }
            if (typeof(T) == typeof(IErrorTag))
            {
                return buffer.Properties.GetOrCreateSingletonProperty(() => new PSharpErrorTagger(buffer) as ITagger<T>);
            }
            throw new InvalidOperationException("Unknown TagType: {typeof(T).FullName}");
        }
    }
}
