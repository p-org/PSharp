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
    internal class PExpressionNode : ExpressionNode
    {
        #region fields

        /// <summary>
        /// Received payloads.
        /// </summary>
        internal List<PPayloadReceiveNode> Payloads;

        /// <summary>
        /// Pending received payloads.
        /// </summary>
        private List<PPayloadReceiveNode> PendingPayloads;

        /// <summary>
        /// The current index.
        /// </summary>
        private int Index;

        #endregion

        #region internal API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="node">Node</param>
        internal PExpressionNode(StatementBlockNode node)
            : base(node)
        {
            this.Payloads = new List<PPayloadReceiveNode>();
            this.PendingPayloads = new List<PPayloadReceiveNode>();
        }

        /// <summary>
        /// Returns the rewritten text.
        /// </summary>
        /// <returns>string</returns>
        internal override string GetRewrittenText()
        {
            if (this.RewrittenStmtTokens.Count == 0 ||
                this.RewrittenStmtTokens.All(val => val == null))
            {
                return "";
            }
            
            return base.TextUnit.Text;
        }

        /// <summary>
        /// Rewrites the syntax node declaration to the intermediate C#
        /// representation.
        /// </summary>
        /// <param name="program">Program</param>
        internal override void Rewrite(IPSharpProgram program)
        {
            if (this.StmtTokens.Count == 0)
            {
                return;
            }
            
            this.Index = 0;
            this.RewrittenStmtTokens = this.StmtTokens.ToList();
            this.PendingPayloads = this.Payloads.ToList();
            this.RunSpecialisedRewrittingPass();
            this.RewriteNextToken(program);
            
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

            base.TextUnit = new TextUnit(text, firstNonNull.TextUnit.Line);
        }

        #endregion

        #region protected API

        /// <summary>
        /// Can be overriden by child classes to implement specialised
        /// rewritting functionality.
        /// </summary>
        protected virtual void RunSpecialisedRewrittingPass()
        {

        }

        /// <summary>
        /// Rewrites the this keyword.
        /// </summary>
        protected void RewriteThis()
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

            this.RewrittenStmtTokens[this.Index] = new Token(new TextUnit(text, line));
        }

        /// <summary>
        /// Rewrites the null keyword.
        /// </summary>
        protected void RewriteNull()
        {
            int line = this.RewrittenStmtTokens[this.Index].TextUnit.Line;
            var text = "default(Machine)";
            this.RewrittenStmtTokens[this.Index] = new Token(new TextUnit(text, line));
        }

        /// <summary>
        /// Rewrites the trigger keyword.
        /// </summary>
        protected void RewriteTrigger()
        {
            int triggerIndex = this.Index;

            int line = this.RewrittenStmtTokens[this.Index].TextUnit.Line;
            var text = "this.Trigger";
            this.RewrittenStmtTokens[this.Index] = new Token(new TextUnit(text, line));

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
            this.RewrittenStmtTokens[this.Index] = new Token(new TextUnit(text, line));

            this.Index = triggerIndex;
        }

        /// <summary>
        /// Rewrites the payload keyword.
        /// </summary>
        protected void RewritePayload()
        {
            int line = this.RewrittenStmtTokens[this.Index].TextUnit.Line;
            var text = "this.Payload";
            this.RewrittenStmtTokens[this.Index] = new Token(new TextUnit(text, line));
        }

        /// <summary>
        /// Rewrites the keys keyword.
        /// </summary>
        protected void RewriteKeys()
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

            this.RewrittenStmtTokens[keysIndex] = new Token(new TextUnit(text, line));
            this.Index = keysIndex;
        }

        /// <summary>
        /// Rewrites the sizeof keyword.
        /// </summary>
        protected void RewriteSizeOf()
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

            this.RewrittenStmtTokens.Insert(this.Index - 1, new Token(new TextUnit(text, line)));
            this.Index = sizeOfIndex - 1;
        }

        /// <summary>
        /// Rewrites the in keyword.
        /// </summary>
        protected void RewriteIn()
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

            this.RewrittenStmtTokens[start] = new Token(new TextUnit(text, line));
            this.Index = start;
        }

        /// <summary>
        /// Rewrites the tuple assignment.
        /// </summary>
        protected void RewriteTupleAssignment()
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
        protected void RewriteMemberIdentifier()
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
            this.RewrittenStmtTokens.Insert(this.Index, new Token(new TextUnit(text, line)));
            this.Index++;
        }

        /// <summary>
        /// Rewrites the cloneable collection in method call.
        /// </summary>
        protected void RewriteCloneableCollectionInMethodCall()
        {
            if (this.Parent.Machine == null || this.Parent.State == null ||
                !(this.Parent.Machine.FunctionDeclarations.Any(val => val.Identifier.TextUnit.Text.
                Equals(this.RewrittenStmtTokens[this.Index].TextUnit.Text))))
            {
                return;
            }

            this.Index++;
            this.SkipWhiteSpaceTokens();

            int counter = 0;
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

                if (this.Parent.Machine.FieldDeclarations.Any(val => val.Identifier.TextUnit.Text.Equals(
                    this.RewrittenStmtTokens[this.Index].TextUnit.Text)))
                {
                    var field = this.Parent.Machine.FieldDeclarations.Find(val =>
                        val.Identifier.TextUnit.Text.Equals(this.RewrittenStmtTokens[this.Index].
                        TextUnit.Text)) as PFieldDeclarationNode;
                    if (field.Type.Type != PType.Tuple &&
                        field.Type.Type != PType.Seq &&
                        field.Type.Type != PType.Map)
                    {
                        return;
                    }

                    var textUnit = new TextUnit("(", this.RewrittenStmtTokens[this.Index].TextUnit.Line);
                    this.RewrittenStmtTokens.Insert(this.Index, new Token(textUnit));
                    this.Index++;

                    this.RewriteMemberIdentifier();

                    var cloneStr = ".Clone() as " + field.Type.GetRewrittenText() + ")";
                    textUnit = new TextUnit(cloneStr, this.RewrittenStmtTokens[this.Index].TextUnit.Line);
                    this.RewrittenStmtTokens.Insert(this.Index + 1, new Token(textUnit));
                }

                this.Index++;
            }
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
                var textUnit = new TextUnit(tupleStr, this.RewrittenStmtTokens[tupleIdx].TextUnit.Line);
                this.RewrittenStmtTokens[tupleIdx] = new Token(textUnit);
            }
            else
            {
                this.RewrittenStmtTokens.RemoveAt(index);
                this.RewrittenStmtTokens.RemoveAt(tupleIdx);
            }
        }

        /// <summary>
        /// Rewrites the any type.
        /// </summary>
        protected void RewriteAnyType()
        {
            var start = this.Index;

            if (this.Parent.Machine == null || this.Parent.State == null ||
                !(this.Parent.Machine.FieldDeclarations.Any(val => val.Identifier.TextUnit.Text.
                Equals(this.RewrittenStmtTokens[this.Index].TextUnit.Text))))
            {
                return;
            }

            var field = this.Parent.Machine.FieldDeclarations.Find(val =>
                val.Identifier.TextUnit.Text.Equals(this.RewrittenStmtTokens[this.Index].
                TextUnit.Text)) as PFieldDeclarationNode;
            if (field.Type.Type != PType.Any)
            {
                return;
            }

            this.Index++;
            this.SkipWhiteSpaceTokens();

            if (this.Index < this.RewrittenStmtTokens.Count &&
                (this.RewrittenStmtTokens[this.Index].Type == TokenType.EqualOp ||
                this.RewrittenStmtTokens[this.Index].Type == TokenType.NotEqualOp))
            {
                var opType = this.RewrittenStmtTokens[this.Index].Type;

                this.Index++;
                this.SkipWhiteSpaceTokens();

                if (this.Index == 0)
                {
                    this.Index = start;
                    return;
                }

                int line = this.RewrittenStmtTokens[this.Index].TextUnit.Line;

                var before = this.Index;
                this.RewriteTupleIndexIdentifier();
                this.RewriteMemberIdentifier();
                this.Index = before;

                var otherObject = "";
                while (this.Index < this.RewrittenStmtTokens.Count)
                {
                    otherObject += this.RewrittenStmtTokens[this.Index].TextUnit.Text;
                    this.Index++;
                }

                while (this.RewrittenStmtTokens[this.Index - 1].Type != opType)
                {
                    this.RewrittenStmtTokens.RemoveAt(this.Index - 1);
                    this.Index--;
                }

                var text = "Convert.ChangeType(";
                this.RewrittenStmtTokens.Insert(start, new Token(new TextUnit(text, line)));
                start++;
                this.Index = start;

                this.RewriteMemberIdentifier();
                this.Index++;
                start = this.Index;

                text = ", " + otherObject + ".GetType())";
                this.RewrittenStmtTokens.Insert(start, new Token(new TextUnit(text, line)));

                text = " " + otherObject;
                this.RewrittenStmtTokens.Add(new Token(new TextUnit(text, line)));

                start++;
            }

            this.Index = start;
        }

        /// <summary>
        /// Rewrites the event type.
        /// </summary>
        protected void RewriteEventType()
        {
            var start = this.Index;

            if (this.Parent.Machine == null || this.Parent.State == null ||
                !(this.Parent.Machine.FieldDeclarations.Any(val => val.Identifier.TextUnit.Text.
                Equals(this.RewrittenStmtTokens[this.Index].TextUnit.Text))))
            {
                return;
            }

            var field = this.Parent.Machine.FieldDeclarations.Find(val =>
                val.Identifier.TextUnit.Text.Equals(this.RewrittenStmtTokens[this.Index].
                TextUnit.Text)) as PFieldDeclarationNode;
            if (field.Type.Type != PType.Event)
            {
                return;
            }

            this.Index++;
            this.SkipWhiteSpaceTokens();

            if (this.Index < this.RewrittenStmtTokens.Count &&
                this.RewrittenStmtTokens[this.Index].Type == TokenType.AssignOp)
            {
                this.Index++;
                this.SkipWhiteSpaceTokens();

                if (this.Index == 0 ||
                    this.RewrittenStmtTokens[this.Index].Type != TokenType.Identifier)
                {
                    this.Index = start;
                    return;
                }

                int line = this.RewrittenStmtTokens[this.Index].TextUnit.Line;
                var text = "typeof(" + this.RewrittenStmtTokens[this.Index].TextUnit.Text + ")";
                this.RewrittenStmtTokens[this.Index] = new Token(new TextUnit(text, line));
            }
            else if (this.Index < this.RewrittenStmtTokens.Count &&
                (this.RewrittenStmtTokens[this.Index].Type == TokenType.EqualOp ||
                this.RewrittenStmtTokens[this.Index].Type == TokenType.NotEqualOp))
            {
                this.Index++;
                this.SkipWhiteSpaceTokens();

                if (this.Index == 0 ||
                    this.RewrittenStmtTokens[this.Index].Type != TokenType.Null)
                {
                    this.Index = start;
                    return;
                }

                int line = this.RewrittenStmtTokens[this.Index].TextUnit.Line;
                var text = "typeof(Default)";
                this.RewrittenStmtTokens[this.Index] = new Token(new TextUnit(text, line));
            }

            this.Index = start;
        }

        /// <summary>
        /// Rewrites the tuple index identifier.
        /// </summary>
        protected void RewriteTupleIndexIdentifier()
        {
            var start = this.Index;

            if (this.Parent.Machine == null || this.Parent.State == null ||
                !(this.Parent.Machine.FieldDeclarations.Any(val => val.Identifier.TextUnit.Text.
                Equals(this.RewrittenStmtTokens[this.Index].TextUnit.Text))))
            {
                return;
            }

            var field = this.Parent.Machine.FieldDeclarations.Find(val =>
                val.Identifier.TextUnit.Text.Equals(this.RewrittenStmtTokens[this.Index].
                TextUnit.Text)) as PFieldDeclarationNode;
            if (field.Type.Type != PType.Tuple)
            {
                return;
            }

            this.Index++;
            this.SkipWhiteSpaceTokens();

            if (this.Index == 0 ||
                this.RewrittenStmtTokens[this.Index].Type != TokenType.Dot)
            {
                this.Index = start;
                return;
            }

            this.Index++;
            this.SkipWhiteSpaceTokens();

            int index = -1;
            if (this.Index < this.RewrittenStmtTokens.Count &&
                int.TryParse(this.RewrittenStmtTokens[this.Index].TextUnit.Text, out index))
            {
                index++;
            }
            else if (this.Index < this.RewrittenStmtTokens.Count && field.Type.Type == PType.Tuple)
            {
                index = (field.Type as PTupleType).NameTokens.FindIndex(val => val.TextUnit.Text.Equals(
                    this.RewrittenStmtTokens[this.Index].TextUnit.Text)) + 1;
            }
            else
            {
                this.Index = start;
                return;
            }

            int line = this.RewrittenStmtTokens[this.Index].TextUnit.Line;
            var text = "Item" + index;
            this.RewrittenStmtTokens[this.Index] = new Token(new TextUnit(text, line));

            this.Index = start;
        }


        private void Foo()
        {

        }


        /// <summary>
        /// Rewrites the insert element at a sequence or a map.
        /// </summary>
        protected void RewriteInsertElementAtCollection()
        {
            this.Index++;
            this.SkipWhiteSpaceTokens();

            int line = this.RewrittenStmtTokens[this.Index].TextUnit.Line;
            var text = "Container.Create";
            this.RewrittenStmtTokens.Insert(this.Index, new Token(new TextUnit(text, line)));

            var start = this.Index;

            this.Index++;
            this.SkipWhiteSpaceTokens();

            int counter = 0;
            while (this.Index < this.RewrittenStmtTokens.Count)
            {
                if (counter > 0 &&
                    this.RewrittenStmtTokens[this.Index].Type == TokenType.LeftParenthesis &&
                    (this.RewrittenStmtTokens[this.Index - 1].Type == TokenType.LeftParenthesis ||
                    this.RewrittenStmtTokens[this.Index - 1].Type == TokenType.Comma))
                {
                    text = "Container.Create";
                    this.RewrittenStmtTokens.Insert(this.Index, new Token(new TextUnit(text, line)));
                    this.Index++;
                }

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

            this.Index = start;
        }

        #endregion

        #region private API

        /// <summary>
        /// Rewrites the next token.
        /// </summary>
        /// <param name="program">Program</param>
        private void RewriteNextToken(IPSharpProgram program)
        {
            if (this.Index == this.RewrittenStmtTokens.Count)
            {
                return;
            }
            
            var token = this.RewrittenStmtTokens[this.Index];
            if (token == null)
            {
                this.RewriteReceivedPayload(program);
            }
            else if (token.Type == TokenType.MachineDecl)
            {
                this.RewriteMachineType();
            }
            else if (token.Type == TokenType.This)
            {
                this.RewriteThis();
            }
            else if (token.Type == TokenType.Null)
            {
                this.RewriteNull();
            }
            else if (token.Type == TokenType.Trigger)
            {
                this.RewriteTrigger();
            }
            else if (token.Type == TokenType.Payload)
            {
                this.RewritePayload();
            }
            else if (token.Type == TokenType.Keys)
            {
                this.RewriteKeys();
            }
            else if (token.Type == TokenType.SizeOf)
            {
                this.RewriteSizeOf();
            }
            else if (token.Type == TokenType.In)
            {
                this.RewriteIn();
            }
            else if (token.Type == TokenType.AssignOp)
            {
                this.RewriteTupleAssignment();
            }
            else if (token.Type == TokenType.NonDeterministic)
            {
                this.RewriteNonDeterministicChoice();
            }
            else if (token.Type == TokenType.InsertOp)
            {
                this.RewriteInsertElementAtCollection();
            }
            else if (token.Type == TokenType.Identifier)
            {
                this.RewriteAnyType();
                this.RewriteEventType();
                this.RewriteTupleIndexIdentifier();
                this.RewriteMemberIdentifier();
                this.RewriteCloneableCollectionInMethodCall();
            }

            this.Index++;
            this.RewriteNextToken(program);
        }

        /// <summary>
        /// Rewrites the received payload.
        /// </summary>
        /// <param name="program">Program</param>
        private void RewriteReceivedPayload(IPSharpProgram program)
        {
            var payload = this.PendingPayloads[0];
            this.PendingPayloads.RemoveAt(0);
            
            payload.Rewrite(program);
            var text = payload.GetRewrittenText();
            var line = payload.TextUnit.Line;

            this.RewrittenStmtTokens[this.Index] = new Token(new TextUnit(text, line));
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
