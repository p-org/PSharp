// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;

using System.Linq;

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

            // Get existing indent from the preceding line.
            var indent = precedingLineText.TakeWhile(c => char.IsWhiteSpace(c)).Select(c => (c == '\t') ? 4 : 1).Sum();

            // Don't increase the indent for the openBracket of the preceding line if its closeBracket is the first non-blank character on the current line.
            var fnbcIsCloseBrace = currentLineText.FirstOrDefault(c => !char.IsWhiteSpace(c)) == RegionParser.CloseCurlyBrace;

            return !fnbcIsCloseBrace && this.RegionParser.GetBoundaryChar(precedingLineText) != RegionParser.BoundaryChar.None
                ? indent + tabSize
                : indent;
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
}
