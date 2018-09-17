// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.PSharp.VisualStudio
{
    /// <summary>
    /// The P# classifier provider.
    /// </summary>
    [Export(typeof(ITaggerProvider))]
    [ContentType("psharp")]
    [TagType(typeof(ClassificationTag))]
    internal sealed class PSharpClassifierProvider : ITaggerProvider
    {
        [Import]
        internal IClassificationTypeRegistryService ClassificationTypeRegistry = null;

        [Import]
        internal IBufferTagAggregatorFactoryService AggregatorFactory = null;

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            var tagAggregator = this.AggregatorFactory.CreateTagAggregator<PSharpTokenTag>(buffer);
            return new PSharpClassifier(buffer, tagAggregator, this.ClassificationTypeRegistry) as ITagger<T>;
        }
    }
}
