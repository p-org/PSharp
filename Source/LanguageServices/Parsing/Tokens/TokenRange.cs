// ------------------------------------------------------------------------------------------------
using System;
using System.Linq;

namespace Microsoft.PSharp.LanguageServices.Parsing
{
    /// <summary>
    /// Represents a range of tokens.
    /// </summary>
    public class TokenRange
    {
        /// <summary>
        /// The token stream being parsed.
        /// </summary>
        private readonly TokenStream TokenStream;

        /// <summary>
        /// The initial token index.
        /// </summary>
        private int StartIndex;

        /// <summary>
        /// The final token index.
        /// </summary>
        private int StopIndex;

        private const int NoStart = -1;
        private const int NoStop = -2;
        private const int PendingStop = -3;

        internal TokenRange(TokenStream tokenStream) => this.TokenStream = tokenStream;

        /// <summary>
        /// Start recording if not already doing so.
        /// </summary>
        internal TokenRange Start()
        {
            if (!this.IsStarted)
            {
                this.StartIndex = this.TokenStream.Index;
                this.StopIndex = PendingStop;
            }

            return this;
        }

        /// <summary>
        /// Stop the range accumulation at the current token.
        /// </summary>
        internal TokenRange Stop()
        {
            if (!this.IsStarted)
            {
                throw new InvalidOperationException("TokenRange is not started.");
            }

            this.StopIndex = this.TokenStream.Index;
            return this;
        }

        /// <summary>
        /// Return the string of the token range.
        /// </summary>
        public string GetString()
        {
            if (!this.IsComplete)
            {
                throw new InvalidOperationException("TokenRange is not complete.");
            }

            return string.Concat(Enumerable.Range(this.StartIndex, this.StopIndex - this.StartIndex + 1)
                .Select(index => this.TokenStream.GetAt(index).TextUnit.Text));
        }

        /// <summary>
        /// Clear the range state.
        /// </summary>
        public void Clear()
        {
            this.StartIndex = NoStart;
            this.StopIndex = NoStop;
        }

        /// <summary>
        /// Return a deep copy of this token range.
        /// </summary>
        internal TokenRange FinishAndClone()
        {
            if (!this.IsStarted)
            {
                throw new InvalidOperationException("TokenRange is not started.");
            }

            this.Stop();
            var result = new TokenRange(this.TokenStream)
            {
                StartIndex = this.StartIndex,
                StopIndex = this.StopIndex
            };

            this.Clear();
            return result;
        }

        /// <summary>
        /// In some cases it is easier to increment this token-by-token.
        /// </summary>
        internal void ExtendStop() => this.StopIndex = this.TokenStream.Index;

        /// <summary>
        /// Verify that the token range is empty.
        /// </summary>
        internal void VerifyIsEmpty()
        {
            // Note: we use this method rather than creating new TokenRange objects at lower levels to
            // provide another level of verification that Stop() is called correctly.
            if (!this.IsEmpty)
            {
                throw new InvalidOperationException("TokenRange is not empty.");
            }
        }

        /// <summary>
        /// True if no range has been specified.
        /// </summary>
        internal bool IsEmpty => this.StartIndex == NoStart && this.StopIndex == NoStop;

        /// <summary>
        /// True if range recording is started and waiting for a stop.
        /// </summary>
        public bool IsStarted => this.StopIndex == PendingStop;

        /// <summary>
        /// True if a complete range (start and stop index) is specified.
        /// </summary>
        public bool IsComplete => this.StartIndex >= 0 && this.StopIndex >= 0;

        /// <summary>
        /// The starting position of the token sequence in the original P# buffer.
        /// </summary>
        public int StartPosition => this.TokenStream.GetAt(this.StartIndex).TextUnit.Start;

        /// <summary>
        /// Compute the length of the string cheaply.
        /// </summary>
        public int StringLength
        {
            get
            {
                var endToken = this.TokenStream.GetAt(this.StopIndex);
                return endToken.TextUnit.Start + endToken.TextUnit.Length - this.StartPosition;
            }
        }

        /// <summary>
        /// Returns a string representation of the object.
        /// </summary>
        public override string ToString()
        {
            var startIndexString = this.StartIndex == NoStart ? nameof(NoStart) : this.StartIndex.ToString();
            var stopIndexString = this.StopIndex == PendingStop
                ? nameof(PendingStop)
                : this.StopIndex == NoStop ? nameof(NoStop) : this.StopIndex.ToString();
            var resultString = this.IsComplete
                ? this.GetString()
                : this.StartIndex >= 0 ? $"{this.TokenStream.GetAt(this.StartIndex).TextUnit.ToString()}..." : "<not started>";
            return $"[{startIndexString} - {stopIndexString}] {resultString}";
        }
    }
}
