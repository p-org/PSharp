// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

namespace Microsoft.PSharp.LanguageServices.Parsing
{
    /// <summary>
    /// P# token types.
    /// </summary>
    public enum TokenType
    {
        /// <summary>
        /// None token.
        /// </summary>
        None = 0,

        /// <summary>
        /// New line token.
        /// </summary>
        NewLine,

        /// <summary>
        /// Whitespace token.
        /// </summary>
        WhiteSpace,

        /// <summary>
        /// Comment token.
        /// </summary>
        Comment,

        /// <summary>
        /// Comment line token.
        /// </summary>
        CommentLine,

        /// <summary>
        /// Comment start token.
        /// </summary>
        CommentStart,

        /// <summary>
        /// Comment end token.
        /// </summary>
        CommentEnd,

        /// <summary>
        /// Region token.
        /// </summary>
        Region,

        /// <summary>
        /// Identifier token.
        /// </summary>
        Identifier,

        /// <summary>
        /// Left curly bracket token.
        /// </summary>
        LeftCurlyBracket,

        /// <summary>
        /// Right curly bracket token.
        /// </summary>
        RightCurlyBracket,

        /// <summary>
        /// Left parenthesis token.
        /// </summary>
        LeftParenthesis,

        /// <summary>
        /// Right parenthesis token.
        /// </summary>
        RightParenthesis,

        /// <summary>
        /// Left square bracket token.
        /// </summary>
        LeftSquareBracket,

        /// <summary>
        /// Right square bracket token.
        /// </summary>
        RightSquareBracket,

        /// <summary>
        /// Left angle bracket token.
        /// </summary>
        LeftAngleBracket,

        /// <summary>
        /// Right angle bracket token.
        /// </summary>
        RightAngleBracket,

        /// <summary>
        /// Machine left curly bracket token.
        /// </summary>
        MachineLeftCurlyBracket,

        /// <summary>
        /// Machine right curly bracket token.
        /// </summary>
        MachineRightCurlyBracket,

        /// <summary>
        /// State left curly bracket token.
        /// </summary>
        StateLeftCurlyBracket,

        /// <summary>
        /// State right curly bracket token.
        /// </summary>
        StateRightCurlyBracket,

        /// <summary>
        /// State group left curly bracket token.
        /// </summary>
        StateGroupLeftCurlyBracket,

        /// <summary>
        /// State group right curly bracket token.
        /// </summary>
        StateGroupRightCurlyBracket,

        /// <summary>
        /// Semicolon token.
        /// </summary>
        Semicolon,

        /// <summary>
        /// Colon token.
        /// </summary>
        Colon,

        /// <summary>
        /// Comma token.
        /// </summary>
        Comma,

        /// <summary>
        /// Dot token.
        /// </summary>
        Dot,

        /// <summary>
        /// Equal op token.
        /// </summary>
        EqualOp,

        /// <summary>
        /// Assign op token.
        /// </summary>
        AssignOp,

        /// <summary>
        /// Insert op token.
        /// </summary>
        InsertOp,

        /// <summary>
        /// Remove op token.
        /// </summary>
        RemoveOp,

        /// <summary>
        /// Not equal op token.
        /// </summary>
        NotEqualOp,

        /// <summary>
        /// Less or equal op token.
        /// </summary>
        LessOrEqualOp,

        /// <summary>
        /// Greater or equal op token.
        /// </summary>
        GreaterOrEqualOp,

        /// <summary>
        /// Lambda op token.
        /// </summary>
        LambdaOp,

        /// <summary>
        /// Plus op token.
        /// </summary>
        PlusOp,

        /// <summary>
        /// Minus op token.
        /// </summary>
        MinusOp,

        /// <summary>
        /// Multiplication op token.
        /// </summary>
        MulOp,

        /// <summary>
        /// Division op token.
        /// </summary>
        DivOp,

        /// <summary>
        /// Mod op token.
        /// </summary>
        ModOp,

        /// <summary>
        /// Logical not token.
        /// </summary>
        LogNotOp,

        /// <summary>
        /// Logical and token.
        /// </summary>
        LogAndOp,

        /// <summary>
        /// Logical or token.
        /// </summary>
        LogOrOp,

        /// <summary>
        /// Private token.
        /// </summary>
        Private,

        /// <summary>
        /// Protected token.
        /// </summary>
        Protected,

        /// <summary>
        /// Internal token.
        /// </summary>
        Internal,

        /// <summary>
        /// Public token.
        /// </summary>
        Public,

        /// <summary>
        /// Partial token.
        /// </summary>
        Partial,

        /// <summary>
        /// Abstract token.
        /// </summary>
        Abstract,

        /// <summary>
        /// Virtual token.
        /// </summary>
        Virtual,

        /// <summary>
        /// Override token.
        /// </summary>
        Override,

        /// <summary>
        /// Namespace token.
        /// </summary>
        NamespaceDecl,

        /// <summary>
        /// Class token.
        /// </summary>
        ClassDecl,

        /// <summary>
        /// Struct token.
        /// </summary>
        StructDecl,

        /// <summary>
        /// Using token.
        /// </summary>
        Using,

        /// <summary>
        /// This token.
        /// </summary>
        This,

        /// <summary>
        /// Base token.
        /// </summary>
        Base,

        /// <summary>
        /// New token.
        /// </summary>
        New,

        /// <summary>
        /// Null token.
        /// </summary>
        Null,

        /// <summary>
        /// True token.
        /// </summary>
        True,

        /// <summary>
        /// False token.
        /// </summary>
        False,

        /// <summary>
        /// In token.
        /// </summary>
        In,

        /// <summary>
        /// As token.
        /// </summary>
        As,

        /// <summary>
        /// Size of token.
        /// </summary>
        SizeOf,

        /// <summary>
        /// If condition token.
        /// </summary>
        IfCondition,

        /// <summary>
        /// Else condition token.
        /// </summary>
        ElseCondition,

        /// <summary>
        /// Do token.
        /// </summary>
        DoLoop,

        /// <summary>
        /// For token.
        /// </summary>
        ForLoop,

        /// <summary>
        /// Foreach token.
        /// </summary>
        ForeachLoop,

        /// <summary>
        /// While token.
        /// </summary>
        WhileLoop,

        /// <summary>
        /// Break token.
        /// </summary>
        Break,

        /// <summary>
        /// Continue token.
        /// </summary>
        Continue,

        /// <summary>
        /// Return token.
        /// </summary>
        Return,

        /// <summary>
        /// Lock token.
        /// </summary>
        Lock,

        /// <summary>
        /// Try token.
        /// </summary>
        Try,

        /// <summary>
        /// Catch token.
        /// </summary>
        Catch,

        /// <summary>
        /// Finally token.
        /// </summary>
        Finally,

        /// <summary>
        /// Async token.
        /// </summary>
        Async,

        /// <summary>
        /// Await token.
        /// </summary>
        Await,

        /// <summary>
        /// Var token.
        /// </summary>
        Var,

        /// <summary>
        /// Void token.
        /// </summary>
        Void,

        /// <summary>
        /// Object token.
        /// </summary>
        Object,

        /// <summary>
        /// String token.
        /// </summary>
        String,

        /// <summary>
        /// Sbyte token.
        /// </summary>
        Sbyte,

        /// <summary>
        /// Byte token.
        /// </summary>
        Byte,

        /// <summary>
        /// Short token.
        /// </summary>
        Short,

        /// <summary>
        /// Ushort token.
        /// </summary>
        Ushort,

        /// <summary>
        /// Int token.
        /// </summary>
        Int,

        /// <summary>
        /// Uint token.
        /// </summary>
        Uint,

        /// <summary>
        /// Long token.
        /// </summary>
        Long,

        /// <summary>
        /// Ulong token.
        /// </summary>
        Ulong,

        /// <summary>
        /// Char token.
        /// </summary>
        Char,

        /// <summary>
        /// Bool token.
        /// </summary>
        Bool,

        /// <summary>
        /// Decimal token.
        /// </summary>
        Decimal,

        /// <summary>
        /// Float token.
        /// </summary>
        Float,

        /// <summary>
        /// Double token.
        /// </summary>
        Double,

        /// <summary>
        /// Machine token.
        /// </summary>
        MachineDecl,

        /// <summary>
        /// Monitor token.
        /// </summary>
        Monitor,

        /// <summary>
        /// State token.
        /// </summary>
        StateDecl,

        /// <summary>
        /// State group token.
        /// </summary>
        StateGroupDecl,

        /// <summary>
        /// Event token.
        /// </summary>
        EventDecl,

        /// <summary>
        /// Start token.
        /// </summary>
        StartState,

        /// <summary>
        /// Hot state token.
        /// </summary>
        HotState,

        /// <summary>
        /// Cold state token.
        /// </summary>
        ColdState,

        /// <summary>
        /// Event identifier token.
        /// </summary>
        EventIdentifier,

        /// <summary>
        /// Machine identifier token.
        /// </summary>
        MachineIdentifier,

        /// <summary>
        /// State identifier token.
        /// </summary>
        StateIdentifier,

        /// <summary>
        /// State group identifier token.
        /// </summary>
        StateGroupIdentifier,

        /// <summary>
        /// Action identifier token.
        /// </summary>
        ActionIdentifier,

        /// <summary>
        /// Type identifier token.
        /// </summary>
        TypeIdentifier,

        /// <summary>
        /// Create machine token.
        /// </summary>
        CreateMachine,

        /// <summary>
        /// Create remote machine token.
        /// </summary>
        CreateRemoteMachine,

        /// <summary>
        /// Send event token.
        /// </summary>
        SendEvent,

        /// <summary>
        /// Raise event token.
        /// </summary>
        RaiseEvent,

        /// <summary>
        /// Jump token.
        /// </summary>
        Jump,

        /// <summary>
        /// Assert token.
        /// </summary>
        Assert,

        /// <summary>
        /// Assume token.
        /// </summary>
        Assume,

        /// <summary>
        /// Pop token.
        /// </summary>
        Pop,

        /// <summary>
        /// On action token.
        /// </summary>
        OnAction,

        /// <summary>
        /// Do action token.
        /// </summary>
        DoAction,

        /// <summary>
        /// Goto state token.
        /// </summary>
        GotoState,

        /// <summary>
        /// Push state token.
        /// </summary>
        PushState,

        /// <summary>
        /// With exit token.
        /// </summary>
        WithExit,

        /// <summary>
        /// Defer event token.
        /// </summary>
        DeferEvent,

        /// <summary>
        /// Ignore event token.
        /// </summary>
        IgnoreEvent,

        /// <summary>
        /// Entry action token.
        /// </summary>
        Entry,

        /// <summary>
        /// Exit action token.
        /// </summary>
        Exit,

        /// <summary>
        /// Trigger token.
        /// </summary>
        Trigger,

        /// <summary>
        /// Halt event token.
        /// </summary>
        HaltEvent,

        /// <summary>
        /// Default event token.
        /// </summary>
        DefaultEvent,

        /// <summary>
        /// Nondeterministic token.
        /// </summary>
        NonDeterministic,

        /// <summary>
        /// "extern" declaration token
        /// </summary>
        ExternDecl
    }
}
