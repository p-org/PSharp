//-----------------------------------------------------------------------
// <copyright file="PExpressionNode.cs">
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
using System.Linq;

namespace Microsoft.PSharp.Parsing.Syntax
{
    /// <summary>
    /// Expression node.
    /// </summary>
    public class PExpressionNode : ExpressionNode
    {
        #region fields

        /// <summary>
        /// Received payloads.
        /// </summary>
        public List<PPayloadReceiveNode> Payloads;

        /// <summary>
        /// Pending received payloads.
        /// </summary>
        private List<PPayloadReceiveNode> PendingPayloads;

        /// <summary>
        /// The current index.
        /// </summary>
        private int Index;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="node">Node</param>
        public PExpressionNode(StatementBlockNode node)
            : base(node)
        {
            this.Payloads = new List<PPayloadReceiveNode>();
            this.PendingPayloads = new List<PPayloadReceiveNode>();
        }

        /// <summary>
        /// Returns the full text.
        /// </summary>
        /// <returns>string</returns>
        public override string GetFullText()
        {
            if (this.StmtTokens.Count == 0 ||
                this.StmtTokens.All(val => val == null))
            {
                return "";
            }
            
            return base.TextUnit.Text;
        }

        /// <summary>
        /// Returns the rewritten text.
        /// </summary>
        /// <returns>string</returns>
        public override string GetRewrittenText()
        {
            if (this.RewrittenStmtTokens.Count == 0 ||
                this.RewrittenStmtTokens.All(val => val == null))
            {
                return "";
            }
            
            return base.RewrittenTextUnit.Text;
        }

        #endregion

        #region internal API

        /// <summary>
        /// Rewrites the syntax node declaration to the intermediate C#
        /// representation.
        /// </summary>
        /// <param name="position">Position</param>
        internal override void Rewrite(ref int position)
        {
            if (this.StmtTokens.Count == 0)
            {
                return;
            }
            
            this.Index = 0;
            this.RewrittenStmtTokens = this.StmtTokens.ToList();
            this.PendingPayloads = this.Payloads.ToList();
            this.RunSpecialisedRewrittingPass(ref position);
            this.RewriteNextToken(ref position);

            var start = position;
            var text = "";

            Token firstNonNull = null;
            foreach (var token in this.RewrittenStmtTokens)
            {
                if (token == null)
                {
                    continue;
                }

                text += token.TextUnit.Text;

                if (firstNonNull == null)
                {
                    firstNonNull = token;
                }
            }

            if (firstNonNull == null)
            {
                return;
            }

            base.RewrittenTextUnit = new TextUnit(text, firstNonNull.TextUnit.Line, start);
            position = base.RewrittenTextUnit.End + 1;
        }

        /// <summary>
        /// Generates a new text unit.
        /// </summary>
        internal override void GenerateTextUnit()
        {
            if (this.StmtTokens.Count == 0)
            {
                return;
            }

            var text = "";

            Token firstNonNull = null;
            foreach (var token in this.StmtTokens)
            {
                if (token == null)
                {
                    continue;
                }

                text += token.TextUnit.Text;

                if (firstNonNull == null)
                {
                    firstNonNull = token;
                }
            }

            if (firstNonNull == null)
            {
                return;
            }

            base.TextUnit = new TextUnit(text, firstNonNull.TextUnit.Line,
                firstNonNull.TextUnit.Start);
        }

        #endregion

        #region protected API

        /// <summary>
        /// Can be overriden by child classes to implement specialised
        /// rewritting functionality.
        /// </summary>
        /// <param name="position">Position</param>
        protected virtual void RunSpecialisedRewrittingPass(ref int position)
        {

        }

        /// <summary>
        /// Rewrites the this keyword.
        /// </summary>
        /// param name="position">Position</param>
        protected void RewriteThis(ref int position)
        {
            if (this.Parent.State == null)
            {
                return;
            }

            int line = this.RewrittenStmtTokens[this.Index].TextUnit.Line;

            var text = "";
            if (this.Parent.Machine.IsMonitor)
            {
                text += "this.Monitor";
            }
            else
            {
                text += "this.Machine";
            }

            this.RewrittenStmtTokens[this.Index] = new Token(new TextUnit(text, line, position));
            position += text.Length;
        }

        /// <summary>
        /// Rewrites the null keyword.
        /// </summary>
        /// param name="position">Position</param>
        protected void RewriteNull(ref int position)
        {
            int line = this.RewrittenStmtTokens[this.Index].TextUnit.Line;
            var text = "default(Machine)";
            this.RewrittenStmtTokens[this.Index] = new Token(new TextUnit(text, line, position));
            position += text.Length;
        }

        /// <summary>
        /// Rewrites the trigger keyword.
        /// </summary>
        /// param name="position">Position</param>
        protected void RewriteTrigger(ref int position)
        {
            int triggerIndex = this.Index;

            int line = this.RewrittenStmtTokens[this.Index].TextUnit.Line;
            var text = "this.Trigger";
            this.RewrittenStmtTokens[this.Index] = new Token(new TextUnit(text, line, position));
            position += text.Length;

            this.Index++;
            this.SkipWhiteSpaceTokens();
            if (this.RewrittenStmtTokens.Count == this.Index ||
                this.RewrittenStmtTokens[this.Index].Type != TokenType.EqualOp)
            {
                this.Index = triggerIndex;
                this.Index--;
                this.SkipWhiteSpaceTokens(true);
                if (this.Index < 0 ||
                    this.RewrittenStmtTokens[this.Index].Type != TokenType.EqualOp)
                {
                    this.Index = triggerIndex;
                    return;
                }
                else
                {
                    this.Index = triggerIndex;
                    this.Index--;
                }
            }
            else
            {
                this.Index++;
                this.SkipWhiteSpaceTokens();
            }

            if (this.Index < 0 ||
                this.RewrittenStmtTokens.Count == this.Index ||
                this.RewrittenStmtTokens[this.Index].Type != TokenType.Identifier)
            {
                this.Index = triggerIndex;
                return;
            }

            line = this.RewrittenStmtTokens[this.Index].TextUnit.Line;
            text = "typeof(" + this.RewrittenStmtTokens[this.Index].TextUnit.Text + ")";
            this.RewrittenStmtTokens[this.Index] = new Token(new TextUnit(text, line, position));
            position += 8;

            this.Index = triggerIndex;
        }

        /// <summary>
        /// Rewrites the payload keyword.
        /// </summary>
        /// param name="position">Position</param>
        protected void RewritePayload(ref int position)
        {
            int line = this.RewrittenStmtTokens[this.Index].TextUnit.Line;
            var text = "this.Payload";
            this.RewrittenStmtTokens[this.Index] = new Token(new TextUnit(text, line, position));
            position += text.Length;
        }

        /// <summary>
        /// Rewrites the keys keyword.
        /// </summary>
        /// param name="position">Position</param>
        protected void RewriteKeys(ref int position)
        {
            var keysIndex = this.Index;

            this.Index++;
            this.SkipWhiteSpaceTokens();
            this.Index++;
            this.SkipWhiteSpaceTokens();

            var map = new List<Token>();
            int counter = 1;

            while (this.Index < this.RewrittenStmtTokens.Count)
            {
                if (this.RewrittenStmtTokens[this.Index].Type == TokenType.LeftParenthesis)
                {
                    counter++;
                }
                else if (this.RewrittenStmtTokens[this.Index].Type == TokenType.RightParenthesis)
                {
                    counter--;
                }

                if (counter == 0)
                {
                    break;
                }

                map.Add(this.RewrittenStmtTokens[this.Index]);
                this.Index++;
            }

            int line = this.RewrittenStmtTokens[this.Index].TextUnit.Line;
            var text = "(";

            foreach (var token in map)
            {
                text += token.TextUnit.Text;
            }

            text += ").Keys";

            while (keysIndex != this.Index)
            {
                this.RewrittenStmtTokens.RemoveAt(this.Index);
                this.Index--;
            }

            this.RewrittenStmtTokens[keysIndex] = new Token(new TextUnit(text, line, position));
            position += text.Length;
            this.Index = keysIndex;
        }

        /// <summary>
        /// Rewrites the sizeof keyword.
        /// </summary>
        /// param name="position">Position</param>
        protected void RewriteSizeOf(ref int position)
        {
            var sizeOfIndex = this.Index;

            int line = this.RewrittenStmtTokens[this.Index].TextUnit.Line;
            var text = ".Count";

            this.RewrittenStmtTokens.RemoveAt(this.Index);
            if (this.RewrittenStmtTokens.Count == this.Index ||
                this.RewrittenStmtTokens[this.Index].Type != TokenType.LeftParenthesis)
            {
                return;
            }

            int leftParenIndex = this.Index;
            this.Index++;

            int counter = 1;
            while (this.Index < this.RewrittenStmtTokens.Count)
            {
                if (this.RewrittenStmtTokens[this.Index].Type == TokenType.LeftParenthesis)
                {
                    counter++;
                }
                else if (this.RewrittenStmtTokens[this.Index].Type == TokenType.RightParenthesis)
                {
                    counter--;
                }

                if (counter == 0)
                {
                    break;
                }

                this.Index++;
            }

            this.RewrittenStmtTokens.RemoveAt(this.Index);
            this.RewrittenStmtTokens.RemoveAt(leftParenIndex);

            this.RewrittenStmtTokens.Insert(this.Index - 1, new Token(new TextUnit(text, line, position)));
            position += text.Length;
            this.Index = sizeOfIndex - 1;
        }

        /// <summary>
        /// Rewrites the in keyword.
        /// </summary>
        /// param name="position">Position</param>
        protected void RewriteIn(ref int position)
        {
            var inIndex = this.Index - 1;
            var start = inIndex;

            var key = new List<Token>();
            while (inIndex >= 0)
            {
                if ((this.RewrittenStmtTokens[inIndex].Type == TokenType.Identifier) ||
                    (this.RewrittenStmtTokens[inIndex].Type == TokenType.None) ||
                    (this.RewrittenStmtTokens[inIndex].Type == TokenType.LeftSquareBracket) ||
                    (this.RewrittenStmtTokens[inIndex].Type == TokenType.RightSquareBracket))
                {
                    key.Add(this.RewrittenStmtTokens[inIndex]);
                    start = inIndex;
                }
                else if ((this.RewrittenStmtTokens[inIndex].Type != TokenType.WhiteSpace) &&
                    (this.RewrittenStmtTokens[inIndex].Type != TokenType.NewLine))
                {
                    break;
                }
                
                inIndex--;
            }

            inIndex = this.Index + 1;
            var end = inIndex;

            var collection = new List<Token>();
            while (inIndex < this.RewrittenStmtTokens.Count)
            {
                if ((this.RewrittenStmtTokens[inIndex].Type == TokenType.Identifier) ||
                    (this.RewrittenStmtTokens[inIndex].Type == TokenType.LeftSquareBracket) ||
                    (this.RewrittenStmtTokens[inIndex].Type == TokenType.RightSquareBracket))
                {
                    collection.Add(this.RewrittenStmtTokens[inIndex]);
                    end = inIndex;
                }
                else if ((this.RewrittenStmtTokens[inIndex].Type != TokenType.WhiteSpace) &&
                    (this.RewrittenStmtTokens[inIndex].Type != TokenType.NewLine))
                {
                    break;
                }

                inIndex++;
            }

            int line = this.RewrittenStmtTokens[start].TextUnit.Line;
            var text = "(";
            
            foreach (var token in collection)
            {
                text += token.TextUnit.Text;
            }

            text += ").Has(";

            key.Reverse();
            foreach (var token in key)
            {
                text += token.TextUnit.Text;
            }

            text += ")";

            while (start != end)
            {
                this.RewrittenStmtTokens.RemoveAt(end);
                end--;
            }

            this.RewrittenStmtTokens[start] = new Token(new TextUnit(text, line, position));
            position += text.Length;
            this.Index = start;
        }

        /// <summary>
        /// Rewrites the default keyword.
        /// </summary>
        /// param name="position">Position</param>
        protected void RewriteDefault(ref int position)
        {
            this.Index++;

            int counter = 0;
            while (this.Index < this.RewrittenStmtTokens.Count)
            {
                if (this.RewrittenStmtTokens[this.Index] != null &&
                    this.RewrittenStmtTokens[this.Index].Type == TokenType.LeftParenthesis)
                {
                    counter++;
                }
                else if (this.RewrittenStmtTokens[this.Index] != null &&
                    this.RewrittenStmtTokens[this.Index].Type == TokenType.RightParenthesis)
                {
                    counter--;
                }
                else if (this.RewrittenStmtTokens[this.Index] != null &&
                    this.RewrittenStmtTokens[this.Index].Type == TokenType.Map)
                {

                }

                if (counter == 0)
                {
                    break;
                }

                this.Index++;
            }
        }

        /// <summary>
        /// Rewrites the tuple assignment.
        /// </summary>
        /// param name="position">Position</param>
        protected void RewriteTupleAssignment(ref int position)
        {
            var assignIndex = this.Index;

            this.Index++;
            this.SkipWhiteSpaceTokens();
            if (this.Index == this.RewrittenStmtTokens.Count ||
                this.RewrittenStmtTokens[this.Index] == null ||
                this.RewrittenStmtTokens[this.Index].Type != TokenType.LeftParenthesis)
            {
                this.Index = assignIndex;
                return;
            }

            int leftParenIndex = this.Index;
            this.Index++;

            int counter = 1;
            while (this.Index < this.RewrittenStmtTokens.Count)
            {
                if (this.RewrittenStmtTokens[this.Index] != null &&
                    this.RewrittenStmtTokens[this.Index].Type == TokenType.LeftParenthesis)
                {
                    counter++;
                }
                else if (this.RewrittenStmtTokens[this.Index] != null && 
                    this.RewrittenStmtTokens[this.Index].Type == TokenType.RightParenthesis)
                {
                    counter--;
                }

                if (counter == 0)
                {
                    break;
                }

                this.Index++;
            }

            if (counter > 0)
            {
                this.Index = assignIndex;
                return;
            }

            this.Index++;
            this.SkipWhiteSpaceTokens();
            if (this.Index < this.RewrittenStmtTokens.Count &&
                this.RewrittenStmtTokens[this.Index] != null &&
                this.RewrittenStmtTokens[this.Index].TextUnit.Text.StartsWith("."))
            {
                this.Index = assignIndex;
                return;
            }

            var index = leftParenIndex;
            this.RewriteTuple(ref index);

            this.Index = assignIndex;
        }

        /// <summary>
        /// Rewrites the member identifier.
        /// </summary>
        /// <param name="position">Position</param>
        protected void RewriteMemberIdentifier(ref int position)
        {
            if (this.Parent.Machine == null || this.Parent.State == null ||
                !(this.Parent.Machine.FieldDeclarations.Any(val => val.Identifier.TextUnit.Text.
                Equals(this.RewrittenStmtTokens[this.Index].TextUnit.Text)) ||
                this.Parent.Machine.FunctionDeclarations.Any(val => val.Identifier.TextUnit.Text.
                Equals(this.RewrittenStmtTokens[this.Index].TextUnit.Text))))
            {
                return;
            }

            int line = this.RewrittenStmtTokens[this.Index].TextUnit.Line;

            var text = "(this.";
            if (this.Parent.Machine.IsMonitor)
            {
                text += "Monitor";
            }
            else
            {
                text += "Machine";
            }

            text += " as " + this.Parent.Machine.Identifier.TextUnit.Text + ").";
            this.RewrittenStmtTokens.Insert(this.Index, new Token(new TextUnit(text, line, position)));
            position += text.Length;
            this.Index++;
        }

        /// <summary>
        /// Rewrites a tuple, recursively.
        /// </summary>
        /// <param name="index">Index</param>
        protected void RewriteTuple(ref int index)
        {
            var tupleIdx = index;
            index++;

            int tupleSize = 1;
            bool expectsComma = false;
            while (index < this.RewrittenStmtTokens.Count &&
                this.RewrittenStmtTokens[index].Type != TokenType.RightParenthesis)
            {
                if (this.RewrittenStmtTokens[index].Type == TokenType.WhiteSpace ||
                    this.RewrittenStmtTokens[index].Type == TokenType.NewLine)
                {
                    index++;
                    continue;
                }

                if ((!expectsComma &&
                    this.RewrittenStmtTokens[index].Type != TokenType.Identifier &&
                    this.RewrittenStmtTokens[index].Type != TokenType.This &&
                    this.RewrittenStmtTokens[index].Type != TokenType.True &&
                    this.RewrittenStmtTokens[index].Type != TokenType.False &&
                    this.RewrittenStmtTokens[index].Type != TokenType.Null &&
                    this.RewrittenStmtTokens[index].Type != TokenType.LeftParenthesis) ||
                    (expectsComma && this.RewrittenStmtTokens[index].Type != TokenType.Comma))
                {
                    break;
                }

                if (this.RewrittenStmtTokens[index].Type == TokenType.Identifier ||
                    this.RewrittenStmtTokens[index].Type == TokenType.This ||
                    this.RewrittenStmtTokens[index].Type == TokenType.True ||
                    this.RewrittenStmtTokens[index].Type == TokenType.Null ||
                    this.RewrittenStmtTokens[index].Type == TokenType.False)
                {
                    index++;
                    expectsComma = true;
                }
                else if (this.RewrittenStmtTokens[index].Type == TokenType.LeftParenthesis)
                {
                    this.RewriteTuple(ref index);
                    index++;
                    expectsComma = true;
                }
                else if (this.RewrittenStmtTokens[index].Type == TokenType.Comma)
                {
                    tupleSize++;
                    expectsComma = false;
                    index++;
                }
            }

            if (tupleSize > 1)
            {
                var tupleStr = "Container.Create(";
                var textUnit = new TextUnit(tupleStr, this.RewrittenStmtTokens[tupleIdx].TextUnit.Line,
                    this.RewrittenStmtTokens[tupleIdx].TextUnit.Start);
                this.RewrittenStmtTokens[tupleIdx] = new Token(textUnit);
            }
            else
            {
                this.RewrittenStmtTokens.RemoveAt(index);
                this.RewrittenStmtTokens.RemoveAt(tupleIdx);
            }
        }

        /// <summary>
        /// Rewrites the tuple index identifier.
        /// </summary>
        /// <param name="position">Position</param>
        protected void RewriteTupleIndexIdentifier(ref int position)
        {
            int index = -1;
            if (!int.TryParse(this.RewrittenStmtTokens[this.Index].TextUnit.Text, out index))
            {
                return;
            }

            index++;
            if (this.Index == 0 ||
                this.RewrittenStmtTokens[this.Index - 1].Type != TokenType.Dot)
            {
                return;
            }

            int line = this.RewrittenStmtTokens[this.Index].TextUnit.Line;
            var text = "Item" + index;
            this.RewrittenStmtTokens[this.Index] = new Token(new TextUnit(text, line, position));
            position += text.Length;
        }

        /// <summary>
        /// Rewrites the insert element at a sequence or a map.
        /// </summary>
        /// <param name="position">Position</param>
        protected void RewriteInsertElementAtCollection(ref int position)
        {
            this.Index++;
            this.SkipWhiteSpaceTokens();

            int line = this.RewrittenStmtTokens[this.Index].TextUnit.Line;
            var text = "Container.Create";
            this.RewrittenStmtTokens.Insert(this.Index, new Token(new TextUnit(text, line, position)));
            position += text.Length;
        }

        #endregion

        #region private API

        /// <summary>
        /// Rewrites the next token.
        /// </summary>
        /// <param name="position">Position</param>
        private void RewriteNextToken(ref int position)
        {
            if (this.Index == this.RewrittenStmtTokens.Count)
            {
                return;
            }

            var token = this.RewrittenStmtTokens[this.Index];
            if (token == null)
            {
                this.RewriteReceivedPayload(ref position);
            }
            else if (token.Type == TokenType.MachineDecl)
            {
                this.RewriteMachineType(ref position);
            }
            else if (token.Type == TokenType.This)
            {
                this.RewriteThis(ref position);
            }
            else if (token.Type == TokenType.Null)
            {
                this.RewriteNull(ref position);
            }
            else if (token.Type == TokenType.Trigger)
            {
                this.RewriteTrigger(ref position);
            }
            else if (token.Type == TokenType.Payload)
            {
                this.RewritePayload(ref position);
            }
            else if (token.Type == TokenType.Keys)
            {
                this.RewriteKeys(ref position);
            }
            else if (token.Type == TokenType.SizeOf)
            {
                this.RewriteSizeOf(ref position);
            }
            else if (token.Type == TokenType.In)
            {
                this.RewriteIn(ref position);
            }
            else if (token.Type == TokenType.DefaultEvent)
            {
                this.RewriteDefault(ref position);
            }
            else if (token.Type == TokenType.AssignOp)
            {
                this.RewriteTupleAssignment(ref position);
            }
            else if (token.Type == TokenType.NonDeterministic)
            {
                this.RewriteNonDeterministicChoice(ref position);
            }
            else if (token.Type == TokenType.InsertOp)
            {
                this.RewriteInsertElementAtCollection(ref position);
            }
            else if (token.Type == TokenType.Identifier)
            {
                this.RewriteTupleIndexIdentifier(ref position);
                this.RewriteMemberIdentifier(ref position);
            }

            this.Index++;
            this.RewriteNextToken(ref position);
        }

        /// <summary>
        /// Rewrites the received payload.
        /// </summary>
        /// param name="position">Position</param>
        private void RewriteReceivedPayload(ref int position)
        {
            var payload = this.PendingPayloads[0];
            this.PendingPayloads.RemoveAt(0);

            payload.GenerateTextUnit();
            payload.Rewrite(ref position);
            var text = payload.GetRewrittenText();
            var line = payload.TextUnit.Line;

            this.RewrittenStmtTokens[this.Index] = new Token(new TextUnit(text, line, position));
            position += text.Length;
        }

        /// <summary>
        /// Skips whitespace tokens.
        /// </summary>
        /// <param name="backwards">Skip backwards</param>
        private void SkipWhiteSpaceTokens(bool backwards = false)
        {
            while (this.Index >= 0 &&
                this.Index < this.RewrittenStmtTokens.Count &&
                this.RewrittenStmtTokens[this.Index] != null &&
                (this.RewrittenStmtTokens[this.Index].Type == TokenType.WhiteSpace ||
                this.RewrittenStmtTokens[this.Index].Type == TokenType.NewLine))
            {
                if (!backwards)
                {
                    this.Index++;
                }
                else
                {
                    this.Index--;
                }
            }

            return;
        }

        #endregion
    }
}
