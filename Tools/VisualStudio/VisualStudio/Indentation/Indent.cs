using System;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
using Microsoft.VisualStudio.TextManager.Interop;

using System.Linq;
using System.Collections.Generic;

namespace Microsoft.PSharp.VisualStudio
{
    /// <summary>
    /// The P# indentation functionality.
    /// </summary>
    internal sealed class Indent : ISmartIndent
    {
        private readonly ITextView textView;
        private bool isDisposed;

        private RegionParser RegionParser = new RegionParser(RegionParser.BoundaryChar.CurlyBrace);

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="textView">ITextView</param>
        public Indent(ITextView textView) => this.textView = textView;

        public int? GetDesiredIndentation(ITextSnapshotLine line) => this.GetLineIndentation(line);

        internal int GetLineIndentation(ITextSnapshotLine line)
        {
            var tabSize = this.textView.Options.GetIndentSize();

            // Get the nearest non-blank preceding line, if any.
            var currentLineText = line.GetText();
            string precedingLineText = string.Empty;
            while (line.LineNumber > 0 && string.IsNullOrWhiteSpace(precedingLineText))
            {
                line = line.Snapshot.GetLineFromLineNumber(line.LineNumber - 1);
                precedingLineText = line.GetText();
            }
            if (line.LineNumber == 0)
            {
                return 0;
            }

            // Get existing indent from the preceding line and trim any extraneous whitespace
            var indent = (int)(precedingLineText.TakeWhile(c => char.IsWhiteSpace(c)).Select(c => (c == '\t') ? tabSize : 1).Sum() / tabSize) * tabSize;

            // Increase the indent if the preceding line ended with an openBracket
            if (this.RegionParser.GetBoundaryChar(precedingLineText) != RegionParser.BoundaryChar.None)
            {
                indent += tabSize;
            }

            // Decrease the indent if a closeBracket is the first non-blank character on the current line.
            return currentLineText.FirstOrDefault(c => !char.IsWhiteSpace(c)) == RegionParser.CloseCurlyBrace
                ? indent -= tabSize
                : indent;
        }

        internal IEnumerable<IndentReplacement> GetSpanIndents(TextSpan span, int tabSize)
        {
            IndentReplacement indentLine(string lineString, int desiredIndentSize, out bool hasTrailingBrace)
            {
                var existingIndentSize = lineString.TakeWhile(c => char.IsWhiteSpace(c)).Select(c => 1).Sum();
                var desiredIndent = string.Empty.PadLeft(desiredIndentSize);

                // Due to potentially mixing tabs and spaces, just replace the existing indent.
                hasTrailingBrace = this.RegionParser.GetBoundaryChar(lineString) != RegionParser.BoundaryChar.None;
                return new IndentReplacement(existingIndentSize, desiredIndent);
            }

            // The first line becomes the preceding line after we get its indent
            var precedingLine = this.textView.TextSnapshot.GetLineFromLineNumber(span.iStartLine);
            var indent = this.GetLineIndentation(precedingLine);
            yield return indentLine(precedingLine.GetText(), indent, out bool prevHasTrailingBrace);

            for (var lineNum = span.iStartLine + 1; lineNum <= span.iEndLine; ++lineNum)
            {
                var currentLineText = this.textView.TextSnapshot.GetLineFromLineNumber(lineNum).GetText();
                var fnbcIsCloseBrace = currentLineText.FirstOrDefault(c => !char.IsWhiteSpace(c)) == RegionParser.CloseCurlyBrace;
                indent = fnbcIsCloseBrace ? indent - tabSize
                        : prevHasTrailingBrace ? indent + tabSize : indent;
                yield return indentLine(currentLineText, indent, out prevHasTrailingBrace);
            }
        }

        public void Dispose()
        {
            if (!this.isDisposed)
            {
                GC.SuppressFinalize(this);
                this.isDisposed = true;
            }
        }
    }

    internal class IndentReplacement
    {
        internal int ExistingIndentLength { get; }
        internal string NewIndent { get; }

        internal IndentReplacement(int existingLen, string newIndent)
        {
            this.ExistingIndentLength = existingLen;
            this.NewIndent = newIndent;
        }
    }
}
