//-----------------------------------------------------------------------
// <copyright file="PSharpSanitizer.cs">
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

using System;
using System.Collections.Generic;

using Microsoft.PSharp.Tooling;

namespace Microsoft.PSharp.Parsing
{
    /// <summary>
    /// The P# sanitizer.
    /// </summary>
    internal class PSharpSanitizer : BaseParser
    {
        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tokens">List of tokens</param>
        public PSharpSanitizer(List<Token> tokens)
            : base(tokens)
        {

        }

        #endregion

        #region protected API

        /// <summary>
        /// Parses the next available token.
        /// </summary>
        protected override void ParseNextToken()
        {
            if (base.Index == base.Tokens.Count)
            {
                return;
            }

            var token = base.Tokens[base.Index];
            switch (token.Type)
            {
                case TokenType.WhiteSpace:
                case TokenType.NewLine:
                    break;

                case TokenType.Comment:
                case TokenType.Region:
                    this.EraseLineComment();
                    break;

                case TokenType.CommentStart:
                    this.EraseMultiLineComment();
                    break;

                case TokenType.Using:
                    this.CheckUsingDirective();
                    break;

                case TokenType.NamespaceDecl:
                    this.CheckNamespaceDeclaration();
                    break;

                case TokenType.RightSquareBracket:
                case TokenType.LeftParenthesis:
                case TokenType.RightParenthesis:
                case TokenType.LeftCurlyBracket:
                case TokenType.CommentEnd:
                    this.ReportParsingError("Invalid use of \"" + token.Text + "\".");
                    break;

                default:
                    this.ReportParsingError("Must be declared inside a namespace.");
                    break;
            }

            base.Index++;
            this.ParseNextToken();
        }

        #endregion

        #region private API

        /// <summary>
        /// Checks a using directive for errors.
        /// </summary>
        private void CheckUsingDirective()
        {
            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            while (base.Index < base.Tokens.Count &&
                base.Tokens[base.Index].Type != TokenType.Semicolon)
            {
                if ((base.Tokens[base.Index].Type != TokenType.Identifier) &&
                    (base.Tokens[base.Index].Type != TokenType.Dot) &&
                    (base.Tokens[base.Index].Type != TokenType.NewLine))
                {
                    this.ReportParsingError("Invalid use of the \"using\" directive.");
                }

                base.Index++;
                base.EraseWhiteSpaceAndCommentTokens();
            }

            if (base.Index == base.Tokens.Count)
            {
                this.ReportParsingError("Invalid use of the \"using\" directive.");
            }
        }

        /// <summary>
        /// Checks a namespace declaration for errors.
        /// </summary>
        private void CheckNamespaceDeclaration()
        {
            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            while (base.Index < base.Tokens.Count &&
                base.Tokens[base.Index].Type != TokenType.LeftCurlyBracket)
            {
                if ((base.Tokens[base.Index].Type != TokenType.Identifier) &&
                    (base.Tokens[base.Index].Type != TokenType.Dot) &&
                    (base.Tokens[base.Index].Type != TokenType.NewLine))
                {
                    this.ReportParsingError("Expected namespace identifier.");
                }

                base.Index++;
                base.EraseWhiteSpaceAndCommentTokens();
            }

            if (base.Index == base.Tokens.Count)
            {
                this.ReportParsingError("Expected \"{\" after the namespace identifier.");
            }

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            this.CheckNextIntraNamespaceDeclaration();
        }

        /// <summary>
        /// Checks the next intra-namespace declration for errors.
        /// </summary>
        private void CheckNextIntraNamespaceDeclaration()
        {
            if (base.Index == base.Tokens.Count)
            {
                return;
            }
            
            bool fixpoint = false;
            var token = base.Tokens[base.Index];
            switch (token.Type)
            {
                case TokenType.WhiteSpace:
                case TokenType.NewLine:
                    break;

                case TokenType.Comment:
                case TokenType.Region:
                    this.EraseLineComment();
                    break;

                case TokenType.CommentStart:
                    this.EraseMultiLineComment();
                    break;

                case TokenType.ClassDecl:
                    this.ReportParsingError("Cannot declare a new class inside a P# file.");
                    break;

                case TokenType.StructDecl:
                    this.ReportParsingError("Cannot declare a new struct inside a P# file.");
                    break;

                case TokenType.Private:
                case TokenType.Protected:
                case TokenType.Internal:
                case TokenType.Public:
                    this.CheckTopLevelAccessModifier();
                    this.CheckTopLevelAbstractModifier();
                    break;

                case TokenType.Abstract:
                    this.CheckTopLevelAbstractModifier();
                    this.CheckTopLevelAccessModifier();
                    break;

                case TokenType.LeftSquareBracket:
                    this.CheckAttributeList();
                    break;

                case TokenType.RightCurlyBracket:
                    fixpoint = true;
                    break;

                case TokenType.RightSquareBracket:
                case TokenType.LeftParenthesis:
                case TokenType.RightParenthesis:
                case TokenType.LeftCurlyBracket:
                case TokenType.CommentEnd:
                    this.ReportParsingError("Invalid use of \"" + token.Text + "\".");
                    break;

                default:
                    this.ReportParsingError("Unexpected declaration.");
                    break;
            }

            base.Index++;

            if (!fixpoint)
            {
                this.CheckNextIntraNamespaceDeclaration();
            }
        }

        /// <summary>
        /// Checks a top level access modifier for errors.
        /// </summary>
        private void CheckTopLevelAccessModifier()
        {
            if ((base.Tokens[base.Index].Type != TokenType.Private) &&
                (base.Tokens[base.Index].Type != TokenType.Protected) &&
                (base.Tokens[base.Index].Type != TokenType.Internal) &&
                (base.Tokens[base.Index].Type != TokenType.Public))
            {
                return;
            }

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();
            if ((base.Tokens[base.Index].Type == TokenType.Private) ||
                (base.Tokens[base.Index].Type == TokenType.Protected) ||
                (base.Tokens[base.Index].Type == TokenType.Internal) ||
                (base.Tokens[base.Index].Type == TokenType.Public))
            {
                this.ReportParsingError("More than one access modifier.");
            }

            this.CheckTopLevelDeclaration();
        }

        /// <summary>
        /// Checks a top level abstract modifier for errors.
        /// </summary>
        private void CheckTopLevelAbstractModifier()
        {
            if (base.Tokens[base.Index].Type != TokenType.Abstract)
            {
                return;
            }

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();
            if (base.Tokens[base.Index].Type == TokenType.Abstract)
            {
                this.ReportParsingError("Duplicate \"abstract\" modifier.");
            }

            this.CheckTopLevelDeclaration();
        }

        /// <summary>
        /// Checks a top level declaration for errors.
        /// </summary>
        private void CheckTopLevelDeclaration()
        {
            if (base.Tokens[base.Index].Type == TokenType.EventDecl)
            {
                this.CheckEventDeclaration();
            }
            else if (base.Tokens[base.Index].Type == TokenType.MachineDecl)
            {
                this.CheckMachineDeclaration();
            }
            else if (base.Tokens[base.Index].Type == TokenType.ClassDecl)
            {
                this.ReportParsingError("Cannot declare a new class inside a P# file.");
            }
            else if (base.Tokens[base.Index].Type == TokenType.StructDecl)
            {
                this.ReportParsingError("Cannot declare a new struct inside a P# file.");
            }
        }

        /// <summary>
        /// Checks an event declaration for errors.
        /// </summary>
        private void CheckEventDeclaration()
        {
            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();
            if (base.Tokens[base.Index].Type != TokenType.Identifier)
            {
                this.ReportParsingError("Expected event identifier.");
            }

            base.Tokens[base.Index] = new Token(base.Tokens[base.Index].Text,
                base.Tokens[base.Index].Line, TokenType.EventIdentifier);

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();
            if (base.Tokens[base.Index].Type != TokenType.Semicolon)
            {
                this.ReportParsingError("Expected \";\".");
            }
        }

        /// <summary>
        /// Checks a machine declaration for errors.
        /// </summary>
        private void CheckMachineDeclaration()
        {
            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();
            if (base.Tokens[base.Index].Type != TokenType.Identifier)
            {
                this.ReportParsingError("Expected machine identifier.");
            }

            base.CurrentMachine = base.Tokens[base.Index].Text;
            base.Tokens[base.Index] = new Token(base.Tokens[base.Index].Text,
                base.Tokens[base.Index].Line, TokenType.MachineIdentifier);

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();
            if (base.Tokens[base.Index].Type == TokenType.Doublecolon)
            {
                base.Index++;
                base.SkipWhiteSpaceAndCommentTokens();

                while (base.Index < base.Tokens.Count &&
                    base.Tokens[base.Index].Type != TokenType.LeftCurlyBracket)
                {
                    if ((base.Tokens[base.Index].Type != TokenType.Identifier) &&
                        (base.Tokens[base.Index].Type != TokenType.Dot) &&
                        (base.Tokens[base.Index].Type != TokenType.NewLine))
                    {
                        this.ReportParsingError("Expected base machine identifier.");
                    }

                    base.Index++;
                    base.EraseWhiteSpaceAndCommentTokens();
                }
            }

            if (base.Tokens[base.Index].Type != TokenType.LeftCurlyBracket)
            {
                this.ReportParsingError("Expected \"{\".");
            }

            base.Tokens[base.Index] = new Token(base.Tokens[base.Index].Text,
                base.Tokens[base.Index].Line, TokenType.MachineLeftCurlyBracket);

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            this.CheckNextIntraMachineDeclaration();
        }

        /// <summary>
        /// Checks the next intra-machine declration for errors.
        /// </summary>
        private void CheckNextIntraMachineDeclaration()
        {
            if (base.Index == base.Tokens.Count)
            {
                return;
            }

            bool fixpoint = false;
            var token = base.Tokens[base.Index];
            switch (token.Type)
            {
                case TokenType.WhiteSpace:
                case TokenType.NewLine:
                    break;

                case TokenType.Comment:
                case TokenType.Region:
                    this.EraseLineComment();
                    break;

                case TokenType.CommentStart:
                    this.EraseMultiLineComment();
                    break;

                case TokenType.ClassDecl:
                    this.ReportParsingError("Cannot declare a new class inside a machine.");
                    break;

                case TokenType.StructDecl:
                    this.ReportParsingError("Cannot declare a new struct inside a machine.");
                    break;

                case TokenType.Private:
                case TokenType.Protected:
                    this.CheckMachineLevelAccessModifier();
                    break;

                case TokenType.Internal:
                case TokenType.Public:
                    this.ReportParsingError("Machine fields, states or actions must be private or protected.");
                    break;

                case TokenType.Abstract:
                    this.ReportParsingError("Machine fields, states or actions cannot be abstract.");
                    break;

                case TokenType.LeftSquareBracket:
                    this.CheckAttributeList();
                    break;

                case TokenType.RightCurlyBracket:
                    base.Tokens[base.Index] = new Token(base.Tokens[base.Index].Text,
                        base.Tokens[base.Index].Line, TokenType.MachineRightCurlyBracket);
                    base.CurrentMachine = "";
                    fixpoint = true;
                    break;

                case TokenType.RightSquareBracket:
                case TokenType.LeftParenthesis:
                case TokenType.RightParenthesis:
                case TokenType.LeftCurlyBracket:
                case TokenType.CommentEnd:
                    this.ReportParsingError("Invalid use of \"" + token.Text + "\".");
                    break;

                default:
                    this.ReportParsingError("Unexpected declaration.");
                    break;
            }

            base.Index++;

            if (!fixpoint)
            {
                this.CheckNextIntraMachineDeclaration();
            }
        }

        /// <summary>
        /// Checks a machine level access modifier for errors.
        /// </summary>
        private void CheckMachineLevelAccessModifier()
        {
            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();
            if ((base.Tokens[base.Index].Type == TokenType.Private) ||
                (base.Tokens[base.Index].Type == TokenType.Protected) ||
                (base.Tokens[base.Index].Type == TokenType.Internal) ||
                (base.Tokens[base.Index].Type == TokenType.Public))
            {
                this.ReportParsingError("More than one access modifier.");
            }
            else if (base.Tokens[base.Index].Type == TokenType.Abstract)
            {
                this.ReportParsingError("Machine fields, states or actions cannot be abstract.");
            }

            if (base.Tokens[base.Index].Type == TokenType.Override)
            {
                base.Index++;
                base.SkipWhiteSpaceAndCommentTokens();
            }

            if (base.Tokens[base.Index].Type == TokenType.StateDecl)
            {
                this.CheckStateDeclaration();
            }
            else if (base.Tokens[base.Index].Type == TokenType.ActionDecl)
            {
                this.CheckActionDeclaration();
            }
            else if (base.Tokens[base.Index].Type == TokenType.ClassDecl)
            {
                this.ReportParsingError("Cannot declare a new class inside a machine.");
            }
            else if (base.Tokens[base.Index].Type == TokenType.StructDecl)
            {
                this.ReportParsingError("Cannot declare a new struct inside a machine.");
            }
            else if (base.Tokens[base.Index].Type == TokenType.Override)
            {
                this.ReportParsingError("Duplicate \"override\" modifier.");
            }
            else if (base.Tokens[base.Index].Type == TokenType.Identifier)
            {
                base.Index++;
                base.SkipWhiteSpaceAndCommentTokens();
                if (base.Tokens[base.Index].Type == TokenType.LessThanOperator)
                {
                    this.CheckLessThanOperator();
                }

                if (base.Tokens[base.Index].Type != TokenType.Identifier)
                {
                    this.ReportParsingError("Expected identifier.");
                }

                if (!ParsingEngine.MachineFieldsAndMethods.ContainsKey(base.CurrentMachine))
                {
                    ParsingEngine.MachineFieldsAndMethods.Add(base.CurrentMachine,
                        new HashSet<string>());
                }

                ParsingEngine.MachineFieldsAndMethods[base.CurrentMachine].Add(base.Tokens[base.Index].Text);

                while (base.Index < base.Tokens.Count &&
                    base.Tokens[base.Index].Type != TokenType.Semicolon &&
                    base.Tokens[base.Index].Type != TokenType.LeftParenthesis)
                {
                    base.Index++;
                    base.SkipWhiteSpaceAndCommentTokens();
                }

                if (base.Tokens[base.Index].Type == TokenType.LeftParenthesis)
                {
                    this.CheckMethodDeclaration();
                }
            }
            else
            {
                this.ReportParsingError("Expected identifier.");
            }
        }

        /// <summary>
        /// Checks a state declaration for errors.
        /// </summary>
        private void CheckStateDeclaration()
        {
            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();
            if (base.Tokens[base.Index].Type != TokenType.Identifier)
            {
                this.ReportParsingError("Expected state identifier.");
            }

            base.CurrentState = base.Tokens[base.Index].Text;
            base.Tokens[base.Index] = new Token(base.Tokens[base.Index].Text,
                base.Tokens[base.Index].Line, TokenType.StateIdentifier);

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();
            if (base.Tokens[base.Index].Type != TokenType.LeftCurlyBracket)
            {
                this.ReportParsingError("Expected \"{\".");
            }

            base.Tokens[base.Index] = new Token(base.Tokens[base.Index].Text,
                base.Tokens[base.Index].Line, TokenType.StateLeftCurlyBracket);

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            this.CheckNextIntraStateDeclaration();
        }

        /// <summary>
        /// Checks the next intra-state declration for errors.
        /// </summary>
        private void CheckNextIntraStateDeclaration()
        {
            if (base.Index == base.Tokens.Count)
            {
                return;
            }

            bool fixpoint = false;
            var token = base.Tokens[base.Index];
            switch (token.Type)
            {
                case TokenType.WhiteSpace:
                case TokenType.NewLine:
                    break;

                case TokenType.Comment:
                case TokenType.Region:
                    this.EraseLineComment();
                    break;

                case TokenType.CommentStart:
                    this.EraseMultiLineComment();
                    break;

                case TokenType.Entry:
                    this.CheckStateEntryDeclaration();
                    break;

                case TokenType.Exit:
                    this.CheckStateExitDeclaration();
                    break;

                case TokenType.OnAction:
                    this.CheckStateActionDeclaration();
                    break;

                case TokenType.DeferEvent:
                    this.CheckDeferEventsDeclaration();
                    break;

                case TokenType.IgnoreEvent:
                    this.CheckIgnoreEventsDeclaration();
                    break;

                case TokenType.Private:
                case TokenType.Protected:
                case TokenType.Internal:
                case TokenType.Public:
                    this.ReportParsingError("State actions cannot have modifiers.");
                    break;

                case TokenType.Abstract:
                    this.ReportParsingError("State actions cannot be abstract.");
                    break;

                case TokenType.LeftSquareBracket:
                    this.CheckAttributeList();
                    break;

                case TokenType.RightCurlyBracket:
                    base.Tokens[base.Index] = new Token(base.Tokens[base.Index].Text,
                        base.Tokens[base.Index].Line, TokenType.StateRightCurlyBracket);
                    base.CurrentState = "";
                    fixpoint = true;
                    break;

                case TokenType.RightSquareBracket:
                case TokenType.LeftParenthesis:
                case TokenType.RightParenthesis:
                case TokenType.LeftCurlyBracket:
                case TokenType.CommentEnd:
                    this.ReportParsingError("Invalid use of \"" + token.Text + "\".");
                    break;

                default:
                    this.ReportParsingError("Unexpected declaration.");
                    break;
            }

            base.Index++;

            if (!fixpoint)
            {
                this.CheckNextIntraStateDeclaration();
            }
        }

        /// <summary>
        /// Checks a state entry declaration for errors.
        /// </summary>
        private void CheckStateEntryDeclaration()
        {
            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();
            if (base.Tokens[base.Index].Type != TokenType.LeftCurlyBracket)
            {
                this.ReportParsingError("Expected \"{\".");
            }

            this.CheckCodeRegion();
        }

        /// <summary>
        /// Checks a state exit declaration for errors.
        /// </summary>
        private void CheckStateExitDeclaration()
        {
            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();
            if (base.Tokens[base.Index].Type != TokenType.LeftCurlyBracket)
            {
                this.ReportParsingError("Expected \"{\".");
            }

            this.CheckCodeRegion();
        }

        /// <summary>
        /// Checks a state action declaration for errors.
        /// </summary>
        private void CheckStateActionDeclaration()
        {
            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();
            if (base.Tokens[base.Index].Type != TokenType.Identifier)
            {
                this.ReportParsingError("Expected event identifier.");
            }

            base.Tokens[base.Index] = new Token(base.Tokens[base.Index].Text,
                base.Tokens[base.Index].Line, TokenType.EventIdentifier);

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();
            if (base.Tokens[base.Index].Type == TokenType.DoAction)
            {
                base.Index++;
                base.SkipWhiteSpaceAndCommentTokens();
                if (base.Tokens[base.Index].Type != TokenType.Identifier)
                {
                    this.ReportParsingError("Expected action identifier.");
                }

                base.Tokens[base.Index] = new Token(base.Tokens[base.Index].Text,
                    base.Tokens[base.Index].Line, TokenType.ActionIdentifier);

                base.Index++;
                base.SkipWhiteSpaceAndCommentTokens();
                if (base.Tokens[base.Index].Type != TokenType.Semicolon)
                {
                    this.ReportParsingError("Expected \";\".");
                }
            }
            else if (base.Tokens[base.Index].Type == TokenType.GotoState)
            {
                base.Index++;
                base.SkipWhiteSpaceAndCommentTokens();
                if (base.Tokens[base.Index].Type != TokenType.Identifier)
                {
                    this.ReportParsingError("Expected state identifier.");
                }

                base.Tokens[base.Index] = new Token(base.Tokens[base.Index].Text,
                    base.Tokens[base.Index].Line, TokenType.StateIdentifier);

                base.Index++;
                base.SkipWhiteSpaceAndCommentTokens();
                if (base.Tokens[base.Index].Type != TokenType.Semicolon)
                {
                    this.ReportParsingError("Expected \";\".");
                }
            }
            else
            {
                this.ReportParsingError("Expected \"do\" or \"goto\".");
            }
        }

        /// <summary>
        /// Checks a defer events declaration for errors.
        /// </summary>
        private void CheckDeferEventsDeclaration()
        {
            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();
            while (base.Index < base.Tokens.Count &&
                base.Tokens[base.Index].Type != TokenType.Semicolon)
            {
                if (base.Tokens[base.Index].Type == TokenType.Identifier)
                {
                    base.Tokens[base.Index] = new Token(base.Tokens[base.Index].Text,
                        base.Tokens[base.Index].Line, TokenType.EventIdentifier);
                }
                else if (base.Tokens[base.Index].Type != TokenType.Comma)
                {
                    this.ReportParsingError("Expected event identifier.");
                }

                base.Index++;
                base.EraseWhiteSpaceAndCommentTokens();
            }
        }

        /// <summary>
        /// Checks an ignore events declaration for errors.
        /// </summary>
        private void CheckIgnoreEventsDeclaration()
        {
            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();
            while (base.Index < base.Tokens.Count &&
                base.Tokens[base.Index].Type != TokenType.Semicolon)
            {
                if (base.Tokens[base.Index].Type == TokenType.Identifier)
                {
                    base.Tokens[base.Index] = new Token(base.Tokens[base.Index].Text,
                        base.Tokens[base.Index].Line, TokenType.EventIdentifier);
                }
                else if (base.Tokens[base.Index].Type != TokenType.Comma)
                {
                    this.ReportParsingError("Expected event identifier.");
                }

                base.Index++;
                base.EraseWhiteSpaceAndCommentTokens();
            }
        }

        /// <summary>
        /// Checks an action declaration for errors.
        /// </summary>
        private void CheckActionDeclaration()
        {
            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();
            if (base.Tokens[base.Index].Type != TokenType.Identifier)
            {
                this.ReportParsingError("Expected action identifier.");
            }

            if (!ParsingEngine.MachineFieldsAndMethods.ContainsKey(base.CurrentMachine))
            {
                ParsingEngine.MachineFieldsAndMethods.Add(base.CurrentMachine,
                    new HashSet<string>());
            }

            ParsingEngine.MachineFieldsAndMethods[base.CurrentMachine].Add(base.Tokens[base.Index].Text);

            base.Tokens[base.Index] = new Token(base.Tokens[base.Index].Text,
                base.Tokens[base.Index].Line, TokenType.ActionIdentifier);

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();
            if (base.Tokens[base.Index].Type != TokenType.LeftCurlyBracket)
            {
                this.ReportParsingError("Expected \"{\".");
            }

            this.CheckCodeRegion();
        }

        /// <summary>
        /// Checks a method declaration for errors.
        /// </summary>
        private void CheckMethodDeclaration()
        {
            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();
            while (base.Index < base.Tokens.Count &&
                base.Tokens[base.Index].Type != TokenType.RightParenthesis)
            {
                base.Tokens[base.Index] = new Token(base.Tokens[base.Index].Text,
                    base.Tokens[base.Index].Line);

                base.Index++;
                base.EraseWhiteSpaceAndCommentTokens();
            }

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();
            if (base.Tokens[base.Index].Type == TokenType.LeftCurlyBracket)
            {
                this.CheckCodeRegion();
            }
            else if (base.Tokens[base.Index].Type != TokenType.Semicolon)
            {
                this.ReportParsingError("Expected \";\" or \"{\".");
            }
        }

        /// <summary>
        /// Checks a code region for errors.
        /// </summary>
        private void CheckCodeRegion()
        {
            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            int bracketCounter = 1;
            while (base.Index < base.Tokens.Count && bracketCounter > 0)
            {
                if (base.Tokens[base.Index].Type == TokenType.LeftCurlyBracket)
                {
                    bracketCounter++;
                }
                else if (base.Tokens[base.Index].Type == TokenType.RightCurlyBracket)
                {
                    bracketCounter--;
                }
                else if (base.Tokens[base.Index].Type == TokenType.DoAction)
                {
                    base.Tokens[base.Index] = new Token(base.Tokens[base.Index].Text,
                        base.Tokens[base.Index].Line, TokenType.DoLoop);
                }
                else if (base.Tokens[base.Index].Type == TokenType.New)
                {
                    this.CheckNewStatement();
                }
                else if (base.Tokens[base.Index].Type == TokenType.CreateMachine)
                {
                    this.CheckCreateStatement();
                }
                else if (base.Tokens[base.Index].Type == TokenType.RaiseEvent)
                {
                    this.CheckRaiseStatement();
                }
                else if (base.Tokens[base.Index].Type == TokenType.SendEvent)
                {
                    this.CheckSendStatement();
                }
                else if (base.Tokens[base.Index].Type == TokenType.DeleteMachine)
                {
                    this.CheckDeleteStatement();
                }

                if (bracketCounter > 0)
                {
                    base.Index++;
                    base.SkipWhiteSpaceAndCommentTokens();
                }
            }
        }

        /// <summary>
        /// Checks a new statement for errors.
        /// </summary>
        private void CheckNewStatement()
        {
            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();
            while (base.Index < base.Tokens.Count &&
                base.Tokens[base.Index].Type != TokenType.LeftParenthesis)
            {
                if (base.Tokens[base.Index].Type == TokenType.Identifier)
                {
                    base.Tokens[base.Index] = new Token(base.Tokens[base.Index].Text,
                        base.Tokens[base.Index].Line, TokenType.TypeIdentifier);
                }
                else if (base.Tokens[base.Index].Type == TokenType.LessThanOperator)
                {
                    this.CheckLessThanOperator();
                }
                else if (base.Tokens[base.Index].Type != TokenType.Dot)
                {
                    this.ReportParsingError("Expected type identifier.");
                }

                base.Index++;
                base.SkipWhiteSpaceAndCommentTokens();
            }

            base.Index--;
        }

        /// <summary>
        /// Checks a create statement for errors.
        /// </summary>
        private void CheckCreateStatement()
        {
            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();
            while (base.Index < base.Tokens.Count &&
                base.Tokens[base.Index].Type != TokenType.LeftCurlyBracket &&
                base.Tokens[base.Index].Type != TokenType.Semicolon)
            {
                if (base.Tokens[base.Index].Type == TokenType.Identifier)
                {
                    base.Tokens[base.Index] = new Token(base.Tokens[base.Index].Text,
                        base.Tokens[base.Index].Line, TokenType.MachineIdentifier);
                }
                else if (base.Tokens[base.Index].Type != TokenType.Dot)
                {
                    this.ReportParsingError("Expected machine identifier.");
                }

                base.Index++;
                base.SkipWhiteSpaceAndCommentTokens();
            }

            if (base.Tokens[base.Index].Type == TokenType.LeftCurlyBracket)
            {
                this.CheckArgumentsList();
            }
            else if (base.Tokens[base.Index].Type != TokenType.Semicolon)
            {
                this.ReportParsingError("Expected \";\" or \"{\".");
            }
        }

        /// <summary>
        /// Checks a raise statement for errors.
        /// </summary>
        private void CheckRaiseStatement()
        {
            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();
            if (base.Tokens[base.Index].Type != TokenType.Identifier)
            {
                this.ReportParsingError("Expected event identifier.");
            }

            base.Tokens[base.Index] = new Token(base.Tokens[base.Index].Text,
                base.Tokens[base.Index].Line, TokenType.EventIdentifier);

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();
            if (base.Tokens[base.Index].Type == TokenType.LeftCurlyBracket)
            {
                this.CheckArgumentsList();
            }

            base.SkipWhiteSpaceAndCommentTokens();
            if (base.Tokens[base.Index].Type != TokenType.Semicolon)
            {
                this.ReportParsingError("Expected \";\".");
            }
        }

        /// <summary>
        /// Checks a send statement for errors.
        /// </summary>
        private void CheckSendStatement()
        {
            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();
            if (base.Tokens[base.Index].Type != TokenType.Identifier)
            {
                this.ReportParsingError("Expected event identifier.");
            }

            base.Tokens[base.Index] = new Token(base.Tokens[base.Index].Text,
                base.Tokens[base.Index].Line, TokenType.EventIdentifier);

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();
            if (base.Tokens[base.Index].Type == TokenType.LeftCurlyBracket)
            {
                this.CheckArgumentsList();
                base.Index++;
            }

            base.SkipWhiteSpaceAndCommentTokens();
            if (base.Tokens[base.Index].Type != TokenType.ToMachine)
            {
                this.ReportParsingError("Expected \"to\".");
            }

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();
            while (base.Index < base.Tokens.Count &&
                base.Tokens[base.Index].Type != TokenType.Semicolon)
            {
                if (base.Tokens[base.Index].Type != TokenType.Identifier &&
                    base.Tokens[base.Index].Type != TokenType.This &&
                    base.Tokens[base.Index].Type != TokenType.Dot)
                {
                    this.ReportParsingError("Expected identifier.");
                }

                base.Index++;
                base.SkipWhiteSpaceAndCommentTokens();
            }
        }

        /// <summary>
        /// Checks a delete statement for errors.
        /// </summary>
        private void CheckDeleteStatement()
        {
            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();
            if (base.Tokens[base.Index].Type != TokenType.Semicolon)
            {
                this.ReportParsingError("Expected \";\".");
            }
        }

        /// <summary>
        /// Checks an attribute list for errors.
        /// </summary>
        private void CheckAttributeList()
        {
            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();
            while (base.Index < base.Tokens.Count &&
                base.Tokens[base.Index].Type != TokenType.RightSquareBracket)
            {
                base.Tokens[base.Index] = new Token(base.Tokens[base.Index].Text,
                    base.Tokens[base.Index].Line);

                base.Index++;
                base.EraseWhiteSpaceAndCommentTokens();
            }
        }

        /// <summary>
        /// Checks an argument list for errors.
        /// </summary>
        private void CheckArgumentsList()
        {
            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();
            while (base.Index < base.Tokens.Count &&
                base.Tokens[base.Index].Type != TokenType.RightCurlyBracket)
            {
                if (base.Tokens[base.Index].Type == TokenType.New)
                {
                    this.CheckNewStatement();
                }

                base.Index++;
                base.EraseWhiteSpaceAndCommentTokens();
            }
        }

        /// <summary>
        /// Checks a less than operator region for errors.
        /// </summary>
        private void CheckLessThanOperator()
        {
            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            this.CheckGenericList();
        }

        /// <summary>
        /// Checks a generic list for errors.
        /// </summary>
        private void CheckGenericList()
        {
            var tokens = new List<Token>();
            var replaceIdx = base.Index;

            while (base.Index < base.Tokens.Count &&
                base.Tokens[base.Index].Type != TokenType.GreaterThanOperator)
            {
                if (base.Tokens[base.Index].Type == TokenType.Semicolon)
                {
                    return;
                }

                tokens.Add(new Token(base.Tokens[replaceIdx].Text, base.Tokens[replaceIdx].Line));
                replaceIdx++;
            }

            foreach (var tok in tokens)
            {
                base.Tokens[base.Index] = tok;
                base.Index++;
            }
        }

        /// <summary>
        /// Erases the line-wide comment.
        /// </summary>
        private void EraseLineComment()
        {
            while (base.Index < base.Tokens.Count &&
                base.Tokens[base.Index].Type != TokenType.NewLine)
            {
                base.Tokens.RemoveAt(base.Index);
            }
        }

        /// <summary>
        /// Erases the line-wide comment.
        /// </summary>
        private void EraseMultiLineComment()
        {
            while (base.Index < base.Tokens.Count &&
                base.Tokens[base.Index].Type != TokenType.CommentEnd)
            {
                base.Tokens.RemoveAt(base.Index);
            }
        }

        /// <summary>
        /// Reports a parting error.
        /// </summary>
        /// <param name="error">Error</param>
        private void ReportParsingError(string error)
        {
            error += " In line " + base.Tokens[base.Index].Line + ":\n";
            //error += this.Lines[this.LineIndex - 1];
            ErrorReporter.ReportErrorAndExit(error);
        }

        #endregion
    }
}
