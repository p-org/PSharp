//-----------------------------------------------------------------------
// <copyright file="TokenType.cs">
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

namespace Microsoft.PSharp.Parsing
{
    /// <summary>
    /// P# token types.
    /// </summary>
    public enum TokenType
    {
        None = 0,

        NewLine,
        WhiteSpace,

        Comment,
        CommentStart,
        CommentEnd,
        Region,

        EventIdentifier,
        MachineIdentifier,
        StateIdentifier,
        ActionIdentifier,
        TypeIdentifier,
        Identifier,

        LeftCurlyBracket,
        RightCurlyBracket,
        LeftParenthesis,
        RightParenthesis,
        LeftSquareBracket,
        RightSquareBracket,

        MachineLeftCurlyBracket,
        MachineRightCurlyBracket,
        StateLeftCurlyBracket,
        StateRightCurlyBracket,

        Semicolon,
        Doublecolon,
        Comma,
        Dot,

        AndOperator,
        OrOperator,
        NotOperator,
        EqualOperator,
        LessThanOperator,
        GreaterThanOperator,
        PlusOperator,
        MinusOperator,
        MultiplyOperator,
        DivideOperator,
        ModOperator,

        Private,
        Protected,
        Internal,
        Public,

        Abstract,
        Virtual,
        Override,

        NamespaceDecl,
        ClassDecl,
        StructDecl,
        Using,

        MachineDecl,
        StateDecl,
        EventDecl,
        ActionDecl,

        OnAction,
        DoAction,
        GotoState,
        DeferEvent,
        IgnoreEvent,
        ToMachine,
        Entry,
        Exit,

        This,
        Base,
        New,
        As,

        ForLoop,
        WhileLoop,
        DoLoop,
        IfCondition,
        ElseCondition,
        Break,
        Continue,
        Return,

        CreateMachine,
        SendEvent,
        RaiseEvent,
        DeleteMachine,
        Assert,
        Payload
    }
}
