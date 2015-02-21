//-----------------------------------------------------------------------
// <copyright file="ClassificationFormats.cs.cs">
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
            this.ForegroundColor = Colors.SteelBlue;
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
}
