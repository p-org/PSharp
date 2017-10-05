using System;
using System.Linq;

namespace Microsoft.PSharp.LanguageServices.Parsing
{
    /// <summary>
    /// Represents a range of tokens
    /// </summary>
    public class TokenRange
    {
        #region fields

        // The token stream being parsed
        private TokenStream tokenStream;

        // The initial token index
        private int startIndex;

        // The final token index
        private int stopIndex;

        const int NoStart = -1;
        const int NoStop = -2;
        const int PendingStop = -3;

        #endregion

        internal TokenRange(TokenStream tokenStream)
        {
            this.tokenStream = tokenStream;
        }

        /// <summary>
        /// Start recording if not already doing so
        /// </summary>
        internal TokenRange Start()
        {
            if (!this.IsStarted)
            {
                this.startIndex = tokenStream.Index;
                this.stopIndex = PendingStop;
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
                throw new InvalidOperationException("TokenRange not started");
            }
            this.stopIndex = this.tokenStream.Index;
            return this;
        }

        /// <summary>
        /// Return the string of the token range.
        /// </summary>
        /// <returns>The string of the token range</returns>
        public string GetString()
        {
            if (!this.IsComplete)
            {
                throw new InvalidOperationException("TokenRange not complete");
            }
            return string.Concat(Enumerable.Range(this.startIndex, this.stopIndex - this.startIndex + 1)
                                           .Select(index => this.tokenStream.GetAt(index).TextUnit.Text));
        }

        /// <summary>
        /// Clear the range state.
        /// </summary>
        public void Clear()
        {
            this.startIndex = NoStart;
            this.stopIndex = NoStop;
        }

        /// <summary>
        /// Return a deep copy of this token range.
        /// </summary>
        /// <returns></returns>
        internal TokenRange FinishAndClone()
        {
            if (!this.IsStarted)
            {
                throw new InvalidOperationException("TokenRange not started");
            }
            this.Stop();
            var result = new TokenRange(this.tokenStream)
            {
                startIndex = this.startIndex,
                stopIndex = this.stopIndex
            };
            this.Clear();
            return result;
        }

        /// <summary>
        /// In some cases it is easier to increment this token-by-token.
        /// </summary>
        internal void ExtendStop()
        {
            this.stopIndex = this.tokenStream.Index;
        }

        /// <summary>
        /// Verify that the token range is empty.
        /// </summary>
        internal void VerifyIsEmpty()
        {
            // Note: We use this method rather than creating new TokenRange objects at lower levels to 
            // provide another level of verification that Stop() is called correctly.
            if (!this.IsEmpty)
            {
                throw new InvalidOperationException("TokenRange is not empty");
            }
        }

        /// <summary>
        /// True if no range has been specified
        /// </summary>
        internal bool IsEmpty { get { return this.startIndex == NoStart && this.stopIndex == NoStop; } }

        /// <summary>
        /// True if range recording is started and waiting for a stop.
        /// </summary>
        public bool IsStarted { get { return this.stopIndex == PendingStop; } }

        /// <summary>
        /// True if a complete range (start and stop index) is specified
        /// </summary>
        public bool IsComplete { get { return this.startIndex >= 0 && this.stopIndex >= 0; } }

        /// <summary>
        /// The starting position of the token sequence in the original P# buffer.
        /// </summary>
        public int StartPosition { get { return this.tokenStream.GetAt(this.startIndex).TextUnit.Start; } }

        /// <summary>
        /// Compute the length of the string cheaply.
        /// </summary>
        public int StringLength
        {
            get
            {
                var endToken = this.tokenStream.GetAt(this.stopIndex);
                return endToken.TextUnit.Start + endToken.TextUnit.Length - this.StartPosition;
            }
        }

        /// <summary>
        /// Returns a string representation of the object.
        /// </summary>
        /// <returns>A string representation of the object.</returns>
        public override string ToString()
        {
            var startIndexString = this.startIndex == NoStart ? nameof(NoStart) : this.startIndex.ToString();
            var stopIndexString = this.stopIndex == PendingStop
                ? nameof(PendingStop)
                : this.stopIndex == NoStop ? nameof(NoStop) : this.stopIndex.ToString();
            var resultString = this.IsComplete
                ? this.GetString()
                : this.startIndex >= 0 ? $"{this.tokenStream.GetAt(this.startIndex).TextUnit.ToString()}..." : "<not started>";
            return $"[{startIndexString} - {stopIndexString}] {resultString}";
        }
    }
}
