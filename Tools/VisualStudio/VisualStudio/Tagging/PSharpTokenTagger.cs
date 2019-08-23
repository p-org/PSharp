// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.PSharp.LanguageServices.Parsing;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.PSharp.VisualStudio
{
    /// <summary>
    /// The P# token tagger.
    /// </summary>
    internal sealed class PSharpTokenTagger : ITagger<PSharpTokenTag>
    {
        readonly ITextBuffer Buffer;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="buffer">ITextBuffer</param>
        internal PSharpTokenTagger(ITextBuffer buffer) => this.Buffer = buffer;

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged
        {
            add { }
            remove { }
        }

        public IEnumerable<ITagSpan<PSharpTokenTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans.Count == 0)
            {
                yield break;
            }

            foreach (var currSpan in spans)
            {
                var containingLine = currSpan.Start.GetContainingLine();

                var tokens = new PSharpLexer().Tokenize(containingLine.GetText());
                this.DetectComment(tokens);
                this.DetectTypeIdentifiers(tokens);

                var currLoc = containingLine.Start.Position;
                foreach (var token in tokens)
                {
                    if (currLoc >= containingLine.End)   // TODOspan: we need to *not* add the ending newline
                    {
                        break;
                    }
                    var tokenSpan = new SnapshotSpan(currSpan.Snapshot, new Span(currLoc, token.Text.Length));
                    yield return new TagSpan<PSharpTokenTag>(tokenSpan, new PSharpTokenTag(token.Type));
                    currLoc += token.Text.Length;
                }
            }
        }

        /// <summary>
        /// Detects a type identifier.
        /// </summary>
        /// <param name="tokens">List of tokens</param>
        private void DetectTypeIdentifiers(List<Token> tokens)
        {
            for (int idx = 0; idx < tokens.Count; idx++)
            {
                if (tokens[idx].Type == TokenType.Identifier)
                {
                    int pre = idx;
                    bool skipWhitespaceBackward()
                    {
                        for (--pre; pre >= 0 && tokens[pre].Type == TokenType.WhiteSpace; --pre)
                        {
                        }

                        // We want to back up to a non-tokenToSkip token
                        return pre >= 0;
                    }

                    if (skipWhitespaceBackward())
                    {
                        if (tokens[pre].Type == TokenType.MachineDecl)
                        {
                            tokens[idx] = tokens[idx].WithType(TokenType.MachineIdentifier);
                        }
                        else if (tokens[pre].Type == TokenType.StateDecl)
                        {
                            tokens[idx] = tokens[idx].WithType(TokenType.StateIdentifier);
                        }
                        else if (tokens[pre].Type == TokenType.EventDecl)
                        {
                            tokens[idx] = tokens[idx].WithType(TokenType.EventIdentifier);
                        }
                        else if (tokens[pre].Type == TokenType.Colon)
                        {
                            // Base class is the same identifier type.
                            if (skipWhitespaceBackward()
                                && (tokens[pre].Type == TokenType.MachineIdentifier
                                    || tokens[pre].Type == TokenType.StateIdentifier
                                    || tokens[pre].Type == TokenType.EventIdentifier)
                                )
                            {
                                tokens[idx] = tokens[idx].WithType(tokens[pre].Type);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Detects a comment.
        /// </summary>
        /// <param name="tokens">List of tokens</param>
        private void DetectComment(List<Token> tokens)
        {
            bool isLineCommentEnabled = false;
            bool isMultiLineCommentEnabled = false;

            for (int idx = 0; idx < tokens.Count; idx++)
            {
                if (isLineCommentEnabled && tokens[idx].Type == TokenType.NewLine)
                {
                    isLineCommentEnabled = false;
                }
                else if (isMultiLineCommentEnabled && tokens[idx].Type == TokenType.CommentEnd)
                {
                    isMultiLineCommentEnabled = false;
                }
                else if (isLineCommentEnabled /*|| isMultiLineCommentEnabled*/) // this does not work here because it does not have full multiline context
                {
                    tokens[idx] = new Token(tokens[idx].TextUnit, TokenType.Comment);
                }

                if (!isMultiLineCommentEnabled && tokens[idx].Type == TokenType.CommentLine)
                {
                    isLineCommentEnabled = true;
                }
                else if (tokens[idx].Type == TokenType.CommentStart)
                {
                    isMultiLineCommentEnabled = true;
                }
            }
        }
    }
}
