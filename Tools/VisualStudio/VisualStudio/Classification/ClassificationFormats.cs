// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.ComponentModel.Composition;
using System.Windows.Media;

using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.PSharp.VisualStudio
{
    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "PSharp.Keyword")]
    [Name("PSharp.Keyword")]
    [UserVisible(true)]
    internal sealed class PSharpKeywordFormat : ClassificationFormatDefinition
    {
        public PSharpKeywordFormat()
        {
            this.ForegroundColor = Colors.CornflowerBlue;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "PSharp.TypeIdentifier")]
    [Name("PSharp.TypeIdentifier")]
    [UserVisible(true)]
    internal sealed class PSharpTypeIdentifierFormat : ClassificationFormatDefinition
    {
        public PSharpTypeIdentifierFormat()
        {
            this.ForegroundColor = Colors.MediumAquamarine;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "PSharp.Comment")]
    [Name("PSharp.Comment")]
    [UserVisible(true)]
    internal sealed class PSharpCommentFormat : ClassificationFormatDefinition
    {
        public PSharpCommentFormat()
        {
            this.ForegroundColor = Colors.SeaGreen;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "PSharp.QuotedString")]
    [Name("PSharp.QuotedString")]
    [UserVisible(true)]
    internal sealed class PSharpQuotedStringFormat : ClassificationFormatDefinition
    {
        public PSharpQuotedStringFormat()
        {
            this.ForegroundColor = Colors.Brown;
        }
    }
}
