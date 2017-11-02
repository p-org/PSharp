//-----------------------------------------------------------------------
// <copyright file="ParsingOptions.cs">
//      Copyright (c) Microsoft Corporation. All rights reserved.
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

namespace Microsoft.PSharp.LanguageServices.Parsing
{
    /// <summary>
    /// The P# parsing options.
    /// </summary>
    public sealed class ParsingOptions
    {
        #region fields

        /// <summary>
        /// The parser should exit when it
        /// finds an error.
        /// </summary>
        internal bool ExitOnError;

        /// <summary>
        /// Enables warnings.
        /// </summary>
        internal bool ShowWarnings;

        /// <summary>
        /// The parser should throw a parsing
        /// exception when it finds an error.
        /// </summary>
        internal bool ThrowParsingException;

        /// <summary>
        /// The parser should skip error checking.
        /// </summary>
        internal bool SkipErrorChecking;

        /// <summary>
        /// Indicates that parsing is being done for the VS Language Service.
        /// </summary>
        internal bool ForVsLanguageService;

        #endregion

        #region constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        private ParsingOptions()
        {
            this.ExitOnError = false;
            this.ShowWarnings = false;
            this.ThrowParsingException = true;
            this.SkipErrorChecking = false;
        }

        #endregion

        #region public methods

        /// <summary>
        /// Creates an instance of the default
        /// P# parsing options.
        /// </summary>
        /// <returns>ParsingOptions</returns>
        public static ParsingOptions CreateDefault()
        {
            return new ParsingOptions();
        }

        /// <summary>
        /// Creates an instance of the default
        /// P# parsing options.
        /// </summary>
        /// <returns>ParsingOptions</returns>
        public static ParsingOptions CreateForVsLanguageService()
        {
            return new ParsingOptions { ForVsLanguageService = true };
        }

        /// <summary>
        /// Enables the option to exit on error.
        /// </summary>
        /// <returns>ParsingOptions</returns>
        public ParsingOptions EnableExitOnError()
        {
            this.ExitOnError = true;
            return this;
        }

        /// <summary>
        /// Disables the option to exit on error.
        /// </summary>
        /// <returns>ParsingOptions</returns>
        public ParsingOptions DisableExitOnError()
        {
            this.ExitOnError = false;
            return this;
        }

        /// <summary>
        /// Enables warnings.
        /// </summary>
        /// <returns>ParsingOptions</returns>
        public ParsingOptions EnableWarnings()
        {
            this.ShowWarnings = true;
            return this;
        }

        /// <summary>
        /// Disables warnings.
        /// </summary>
        /// <returns>ParsingOptions</returns>
        public ParsingOptions DisableWarnings()
        {
            this.ShowWarnings = false;
            return this;
        }

        /// <summary>
        /// Enables the option to throw a parsing exception.
        /// </summary>
        /// <returns>ParsingOptions</returns>
        public ParsingOptions EnableThrowParsingException()
        {
            this.ThrowParsingException = true;
            return this;
        }

        /// <summary>
        /// Disables the option to throw a parsing exception.
        /// </summary>
        /// <returns>ParsingOptions</returns>
        public ParsingOptions DisableThrowParsingException()
        {
            this.ThrowParsingException = false;
            return this;
        }

        /// <summary>
        /// Enables the option to skip error checking.
        /// </summary>
        /// <returns>ParsingOptions</returns>
        public ParsingOptions EnableSkipErrorChecking()
        {
            this.SkipErrorChecking = true;
            return this;
        }

        /// <summary>
        /// Disables the option to skip error checking.
        /// </summary>
        /// <returns>ParsingOptions</returns>
        public ParsingOptions DisableSkipErrorChecking()
        {
            this.SkipErrorChecking = false;
            return this;
        }

        #endregion
    }
}
