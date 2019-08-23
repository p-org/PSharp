// ------------------------------------------------------------------------------------------------

using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.PSharp.VisualStudio
{
    internal class RegionParser
    {
        [Flags] internal enum BoundaryChar
        {
            None = 0x0000,
            CurlyBrace = 0x0001,
            SquareBrace = 0x0002,
            Parenthesis = 0x0004,
            All = CurlyBrace | SquareBrace | Parenthesis
        }

        internal const char OpenCurlyBrace = '{';
        internal const char CloseCurlyBrace = '}';
        internal const char OpenSquareBrace = '[';
        internal const char CloseSquareBrace = ']';
        internal const char OpenParenthesis = '(';
        internal const char CloseParenthesis = ')';

        internal const string LineComment = "//";    // These are the "consume until close" characters
        private const string openComment = "/*";
        private const string closeComment = "*/";
        private const string openQuote = "\"";
        private const string closeQuote = "\\\"";   // '\\' is treated as an escape (a 0-length negative lookahead)

        private readonly List<Region> regions = new List<Region>();
        private readonly BoundaryChar boundaryChars;

        internal Region[] CurlyBraceRegions;
        internal Region[] SquareBraceRegions;
        internal Region[] ParenthesisRegions;

        // In-progress variables.
        readonly Stack<PartialRegion> partialRegions = new Stack<PartialRegion>();
        string consumeClose;

        internal RegionParser(BoundaryChar boundaryChars)
        {
            this.Clear();
            this.boundaryChars = boundaryChars;
        }

        internal void Parse(ITextSnapshot snapshot)
        {
            this.Clear();
            foreach (var line in snapshot.Lines)
            {
                this.ProcessLine(line);
            }
            this.CurlyBraceRegions = this.regions.Where(region => region.BoundaryChar == BoundaryChar.CurlyBrace).ToArray();
            this.SquareBraceRegions = this.regions.Where(region => region.BoundaryChar == BoundaryChar.SquareBrace).ToArray();
            this.ParenthesisRegions = this.regions.Where(region => region.BoundaryChar == BoundaryChar.Parenthesis).ToArray();
        }

        internal BoundaryChar GetBoundaryChar(string lineText)
        {
            this.Clear();
            this.ProcessLine(lineText, 0);
            return this.partialRegions.Count > 0 ? this.partialRegions.Peek().BoundaryChar : BoundaryChar.None;
        }

        internal static bool IsOpenChar(char c) => c == OpenCurlyBrace || c == OpenSquareBrace || c == OpenParenthesis;
        internal static bool IsCloseChar(char c) => c == CloseCurlyBrace || c == CloseSquareBrace || c == CloseParenthesis;

        private void Clear()
        {
            this.regions.Clear();
            this.CurlyBraceRegions = Array.Empty<Region>();
            this.SquareBraceRegions = Array.Empty<Region>();
            this.ParenthesisRegions = Array.Empty<Region>();
            this.partialRegions.Clear();
            this.consumeClose = null;
        }

        private void ProcessLine(ITextSnapshotLine line)
        {
            string text = line.GetText();
            var lineNumber = line.LineNumber;
            ProcessLine(text, lineNumber);
        }

        private void ProcessLine(string text, int lineNumber)
        {
            for (var ii = 0; ii < text.Length; /* incremented in loop */)
            {
                if (MatchToEnd(text, ref ii, this.consumeClose))
                {
                    this.consumeClose = null;
                    continue;
                }
                if (MatchAt(text, ref ii, LineComment))
                {
                    break;
                }
                if (MatchAt(text, ref ii, openComment))
                {
                    this.consumeClose = closeComment;
                    continue;
                }
                if (MatchAt(text, ref ii, openQuote))
                {
                    this.consumeClose = closeQuote;
                    continue;
                }

                if (CheckRegionBoundary(text[ii], ii, lineNumber, BoundaryChar.CurlyBrace, OpenCurlyBrace, CloseCurlyBrace)
                    || CheckRegionBoundary(text[ii], ii, lineNumber, BoundaryChar.SquareBrace, OpenSquareBrace, CloseSquareBrace)
                    || CheckRegionBoundary(text[ii], ii, lineNumber, BoundaryChar.Parenthesis, OpenParenthesis, CloseParenthesis))
                {
                    ++ii;
                    continue;
                }

                // No special handling, so increment and continue
                ++ii;
            }
        }

        private bool CheckRegionBoundary(char cc, int ii, int lineNumber, BoundaryChar boundaryChar, char openChar, char closeChar)
        {
            if (this.boundaryChars.HasFlag(boundaryChar))
            {
                if (cc == openChar)
                {
                    this.partialRegions.Push(new PartialRegion(boundaryChar, lineNumber, ii));
                    return true;
                }

                if (cc == closeChar)
                {
                    if (partialRegions.Count > 0)
                    {
                        PartialRegion partialRegion = partialRegions.Pop();
                        // Uncomment if investigating a bug:  Debug.Assert(partialRegion.BoundaryChar == boundaryChar);
                        this.regions.Add(new Region(partialRegion, lineNumber, ii + 1));
                    }
                    return true;
                }
            }
            return false;
        }

        private bool MatchToEnd(string str, ref int idx, string find)
        {
            if (find != null)
            {
                for (var ii = idx; ii < str.Length; ++ii)
                {
                    var incrementedIdx = ii;
                    if (MatchAt(str, ref incrementedIdx, find))
                    {
                        idx = incrementedIdx;
                        return true;
                    }
                }
            }
            return false;
        }

        private bool MatchAt(string str, ref int idx, string find)
        {
            bool isEscape = find[0] == '\\';
            var findOffset = isEscape ? 1 : 0;
            var findLength = find.Length - findOffset;
            Debug.Assert(findLength <= 2);

            if (idx + findLength <= str.Length
                && (!isEscape || str[idx] != find[0])
                && str[idx] == find[findOffset]
                && (findLength == 1 || str[idx + 1] == find[findOffset + 1]))
            {
                idx += findLength;
                return true;
            }
            return false;
        }
    }

    internal class PartialRegion
    {
        internal int StartLineNumber { get; }
        internal int StartOffset { get; }
        internal RegionParser.BoundaryChar BoundaryChar { get; }

        internal PartialRegion(RegionParser.BoundaryChar boundaryChar, int startLineNumber, int startOffset)
        {
            this.BoundaryChar = boundaryChar;
            this.StartLineNumber = startLineNumber;
            this.StartOffset = startOffset;
        }
    }

    internal class Region : PartialRegion
    {
        public int EndLineNumber { get; set; }
        public int EndOffset { get; set; }

        internal Region(PartialRegion partialRegion, int endLineNumber, int endOffset)
            : base(partialRegion.BoundaryChar, partialRegion.StartLineNumber, partialRegion.StartOffset)
        {
            this.EndLineNumber = endLineNumber;
            this.EndOffset = endOffset;
        }
    }
}
