using
namespace Microsoft.PSharp.LanguageServices.Parsing
{
    /// <summary>
    /// The P# parsing options.
    /// </summary>
    public sealed class ParsingOptions
    {
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
        /// Indicates that parsing is being done for the VS language service.
        /// </summary>
        internal bool IsParsingForVsLanguageService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ParsingOptions"/> class.
        /// </summary>
        private ParsingOptions()
        {
            this.ExitOnError = false;
            this.ShowWarnings = false;
            this.ThrowParsingException = true;
            this.SkipErrorChecking = false;
            this.IsParsingForVsLanguageService = false;
        }

        /// <summary>
        /// Creates an instance of the default P# parsing options.
        /// </summary>
        public static ParsingOptions CreateDefault() => new ParsingOptions();

        /// <summary>
        /// Creates an instance of the P# parsing options for the VS language service.
        /// </summary>
        public static ParsingOptions CreateForVsLanguageService() =>
            new ParsingOptions
            {
                IsParsingForVsLanguageService = true
            };

        /// <summary>
        /// Enables the option to exit on error.
        /// </summary>
        public ParsingOptions EnableExitOnError()
        {
            this.ExitOnError = true;
            return this;
        }

        /// <summary>
        /// Disables the option to exit on error.
        /// </summary>
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
        public ParsingOptions DisableWarnings()
        {
            this.ShowWarnings = false;
            return this;
        }

        /// <summary>
        /// Enables the option to throw a parsing exception.
        /// </summary>
        public ParsingOptions EnableThrowParsingException()
        {
            this.ThrowParsingException = true;
            return this;
        }

        /// <summary>
        /// Disables the option to throw a parsing exception.
        /// </summary>
        public ParsingOptions DisableThrowParsingException()
        {
            this.ThrowParsingException = false;
            return this;
        }

        /// <summary>
        /// Enables the option to skip error checking.
        /// </summary>
        public ParsingOptions EnableSkipErrorChecking()
        {
            this.SkipErrorChecking = true;
            return this;
        }

        /// <summary>
        /// Disables the option to skip error checking.
        /// </summary>
        public ParsingOptions DisableSkipErrorChecking()
        {
            this.SkipErrorChecking = false;
            return this;
        }
    }
}
