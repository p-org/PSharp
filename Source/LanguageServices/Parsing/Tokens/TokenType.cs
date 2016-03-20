//-----------------------------------------------------------------------
// <copyright file="TokenType.cs">
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
    /// P# token types.
    /// </summary>
    public enum TokenType
    {
        None = 0,

        NewLine,
        WhiteSpace,

        #region comments

        Comment,
        CommentLine,
        CommentStart,
        CommentEnd,
        Region,

        #endregion

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

        LambdaOp,

        PlusOp,
        MinusOp,
        MulOp,
        DivOp,
        ModOp,

        LogNotOp,
        LogAndOp,
        LogOrOp,

        #region C#-specific tokens

        Private,
        Protected,
        Internal,
        Public,
        Partial,
        Abstract,
        Virtual,
        Override,

        NamespaceDecl,
        ClassDecl,
        StructDecl,
        Using,

        This,
        Base,
        New,
        Null,
        True,
        False,

        In,
        As,
        SizeOf,

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
        Try,
        Catch,
        Finally,

        Async,
        Await,

        Var,
        Void,
        Object,
        String,
        Sbyte,
        Byte,
        Short,
        Ushort,
        Int,
        Uint,
        Long,
        Ulong,
        Char,
        Bool,
        Decimal,
        Float,
        Double,

        #endregion

        #region P#-specific tokens

        MachineDecl,
        Monitor,
        StateDecl,
        EventDecl,
        StartState,
        HotState,
        ColdState,

        EventIdentifier,
        MachineIdentifier,
        StateIdentifier,
        ActionIdentifier,
        TypeIdentifier,

        CreateMachine,
        CreateRemoteMachine,
        SendEvent,
        ToMachine,
        RaiseEvent,
        Jump,
        Assert,
        Assume,
        Pop,
        
        OnAction,
        DoAction,
        GotoState,
        PushState,
        WithExit,
        DeferEvent,
        IgnoreEvent,
        Entry,
        Exit,

        Trigger,

        HaltEvent,
        DefaultEvent,

        NonDeterministic,

        #endregion
    }
}
