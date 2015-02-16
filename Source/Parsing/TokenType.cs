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
    internal enum TokenType
    {
        None = 0,
        NewLine,
        WhiteSpace,

        LeftCurlyBracket,
        RightCurlyBracket,
        LeftParenthesis,
        RightParenthesis,
        LeftSquareBracket,
        RightSquareBracket,

        MachineLeftCurlyBracket,
        MachineRightCurlyBracket,

        Semicolon,
        Doublecolon,
        Comma,
        Dot,

        LessThanOperator,
        GreaterThanOperator,

        Private,
        Protected,
        Internal,
        Public,

        Abstract,
        Virtual,
        Override,

        NamespaceDecl,
        ClassDecl,
        Using,

        MachineDecl,
        StateDecl,
        EventDecl,
        ActionDecl,

        OnAction,
        DoAction,
        GotoState,
        Entry,
        Exit,

        This,
        Base,
        New,

        ForLoop,
        WhileLoop,
        DoLoop,
        IfCondition,
        ElseCondition,

        SendEvent,
        RaiseEvent,
        DeleteMachine
    }
}
