using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.PSharp.VisualStudio
{
    internal sealed class ClassificationTypeDefinitions
    {
        [Export]
        [ClassificationType(ClassificationTypeNames = "PSharp.Keyword")]
        [Name("PSharp.Keyword")]
        [UserVisible(true)]
        [BaseDefinition(PredefinedClassificationTypeNames.Keyword)]
        internal readonly ClassificationTypeDefinition PSharpKeywordFormat;

        [Export]
        [ClassificationType(ClassificationTypeNames = "PSharp.TypeIdentifier")]
        [Name("PSharp.TypeIdentifier")]
        [UserVisible(true)]
        [BaseDefinition(PredefinedClassificationTypeNames.Identifier)]
        internal readonly ClassificationTypeDefinition PSharpTypeIdentifierFormat;

        [Export]
        [ClassificationType(ClassificationTypeNames = "PSharp.Comment")]
        [Name("PSharp.Comment")]
        [UserVisible(true)]
        [BaseDefinition(PredefinedClassificationTypeNames.Comment)]
        internal readonly ClassificationTypeDefinition PSharpCommentFormat;

        [Export]
        [ClassificationType(ClassificationTypeNames = "PSharp.QuotedString")]
        [Name("PSharp.QuotedString")]
        [UserVisible(true)]
        [BaseDefinition(PredefinedClassificationTypeNames.String)]
        internal readonly ClassificationTypeDefinition PSharpQuotedStringFormat;
        
        [Export]
        [Name(BraceMatchingName)]
        [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
        internal readonly ClassificationTypeDefinition BraceMatching;


        internal const string BraceMatchingName = "brace matching";
    }
}
