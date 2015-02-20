//-----------------------------------------------------------------------
// <copyright file="ClassificationTypes.cs">
//      Copyright (c) 2015 Pantazis Deligiannis (p.deligiannis@imperial.ac.uk)
// 
//      THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//      EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//      MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//      IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//      CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//      TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//      SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
//-----------------------------------------------------------------------

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.PSharp.VisualStudio
{
    /// <summary>
    /// Exports the classification type definitions.
    /// </summary>
    internal static class ClassificationTypes
    {
        [Export(typeof(ClassificationTypeDefinition))]
        [Name("PSharp")]
        internal static ClassificationTypeDefinition PSharpClassificationDefinition = null;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("PSharp.None")]
        internal static ClassificationTypeDefinition PSharpNoneDefinition = null;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("PSharp.Keyword")]
        internal static ClassificationTypeDefinition PSharpKeywordDefinition = null;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("PSharp.NewLine")]
        internal static ClassificationTypeDefinition PSharpNewLineDefinition = null;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("PSharp.WhiteSpace")]
        internal static ClassificationTypeDefinition PSharpWhiteSpaceDefinition = null;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("PSharp.Comment")]
        internal static ClassificationTypeDefinition PSharpCommentDefinition = null;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("PSharp.Region")]
        internal static ClassificationTypeDefinition PSharpRegionDefinition = null;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("PSharp.Identifier")]
        internal static ClassificationTypeDefinition PSharpIdentifierDefinition = null;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("PSharp.LeftCurlyBracket")]
        internal static ClassificationTypeDefinition PSharpLeftCurlyBracketDefinition = null;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("PSharp.RightCurlyBracket")]
        internal static ClassificationTypeDefinition PSharpRightCurlyBracketDefinition = null;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("PSharp.LeftParenthesis")]
        internal static ClassificationTypeDefinition PSharpLeftParenthesisDefinition = null;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("PSharp.RightParenthesis")]
        internal static ClassificationTypeDefinition PSharpRightParenthesisDefinition = null;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("PSharp.LeftSquareBracket")]
        internal static ClassificationTypeDefinition PSharpLeftSquareBracketDefinition = null;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("PSharp.RightSquareBracket")]
        internal static ClassificationTypeDefinition PSharpRightSquareBracketDefinition = null;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("PSharp.Semicolon")]
        internal static ClassificationTypeDefinition PSharpSemicolonDefinition = null;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("PSharp.Doublecolon")]
        internal static ClassificationTypeDefinition PSharpDoublecolonDefinition = null;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("PSharp.Comma")]
        internal static ClassificationTypeDefinition PSharpCommaDefinition = null;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("PSharp.Dot")]
        internal static ClassificationTypeDefinition PSharpDotDefinition = null;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("PSharp.Operator")]
        internal static ClassificationTypeDefinition PSharpOperatorDefinition = null;
    }
}
