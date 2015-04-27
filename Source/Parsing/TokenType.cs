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
        CommentLine,
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
        LeftAngleBracket,
        RightAngleBracket,

        MachineLeftCurlyBracket,
        MachineRightCurlyBracket,
        StateLeftCurlyBracket,
        StateRightCurlyBracket,

        Semicolon,
        Colon,
        Comma,
        Dot,

        EqualOp,
        AssignOp,
        InsertOp,
        RemoveOp,
        NotEqualOp,
        LessOrEqualOp,
        GreaterOrEqualOp,

        PlusOp,
        MinusOp,
        MulOp,
        DivOp,
        ModOp,

        LogNotOp,
        LogAndOp,
        LogOrOp,

        NonDeterministic,

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
        ModelDecl,
        Monitor,
        StateDecl,
        EventDecl,
        ActionDecl,
        FunDecl,

        MainMachine,
        StartState,

        OnAction,
        DoAction,
        GotoState,
        PushState,
        WithExit,
        DeferEvent,
        IgnoreEvent,
        ToMachine,
        Entry,
        Exit,

        This,
        Base,
        New,
        Null,
        True,
        False,

        SizeOf,
        In,
        As,
        Keys,
        Values,

        IfCondition,
        ElseCondition,
        DoLoop,
        ForLoop,
        ForeachLoop,
        WhileLoop,
        Break,
        Continue,
        Return,
        Lock,

        CreateMachine,
        SendEvent,
        RaiseEvent,
        DeleteMachine,
        Assert,
        Assume,
        Payload,
        Trigger,

        HaltEvent,
        DefaultEvent,

        ColdState,
        HotState,

        Var,
        Int,
        Bool,
        Foreign,
        Any,
        Seq,
        Map
    }
}
