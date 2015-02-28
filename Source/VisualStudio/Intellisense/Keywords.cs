//-----------------------------------------------------------------------
// <copyright file="Keywords.cs">
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

namespace Microsoft.PSharp.VisualStudio
{
    internal static class Keywords
    {
        /// <summary>
        /// Returns the P# keywords.
        /// </summary>
        /// <returns>Dictionary of keywords</returns>
        public static Dictionary<string, Tuple<string>> Get()
        {
            var keywords = new Dictionary<string, Tuple<string>>();

            keywords.Add("private", new Tuple<string>("private Keyword"));
            keywords.Add("protected", new Tuple<string>("protected Keyword"));
            keywords.Add("internal", new Tuple<string>("internal Keyword"));
            keywords.Add("public", new Tuple<string>("public Keyword"));
            keywords.Add("abstract", new Tuple<string>("abstract Keyword"));
            keywords.Add("virtual", new Tuple<string>("virtual Keyword"));
            keywords.Add("override", new Tuple<string>("override Keyword"));

            keywords.Add("namespace", new Tuple<string>("namespace Keyword"));
            keywords.Add("using", new Tuple<string>("using Keyword"));

            keywords.Add("main", new Tuple<string>("main Keyword"));
            keywords.Add("start", new Tuple<string>("start Keyword"));

            keywords.Add("machine", new Tuple<string>("machine Keyword"));
            keywords.Add("model", new Tuple<string>("model Keyword"));
            keywords.Add("monitor", new Tuple<string>("monitor Keyword"));
            keywords.Add("state", new Tuple<string>("state Keyword"));
            keywords.Add("event", new Tuple<string>("event Keyword"));
            keywords.Add("action", new Tuple<string>("action Keyword"));
            keywords.Add("fun", new Tuple<string>("fun Keyword"));

            keywords.Add("on", new Tuple<string>("on Keyword"));
            keywords.Add("do", new Tuple<string>("do Keyword"));
            keywords.Add("goto", new Tuple<string>("goto Keyword"));
            keywords.Add("defer", new Tuple<string>("defer Keyword"));
            keywords.Add("ignore", new Tuple<string>("ignore Keyword"));
            keywords.Add("to", new Tuple<string>("to Keyword"));
            keywords.Add("entry", new Tuple<string>("entry Keyword"));
            keywords.Add("exit", new Tuple<string>("exit Keyword"));

            keywords.Add("this", new Tuple<string>("this Keyword"));
            keywords.Add("base", new Tuple<string>("base Keyword"));
            keywords.Add("new", new Tuple<string>("new Keyword"));
            keywords.Add("null", new Tuple<string>("null Keyword"));
            keywords.Add("true", new Tuple<string>("true Keyword"));
            keywords.Add("false", new Tuple<string>("false Keyword"));

            keywords.Add("sizeof", new Tuple<string>("sizeof Keyword"));
            keywords.Add("in", new Tuple<string>("in Keyword"));
            keywords.Add("as", new Tuple<string>("as Keyword"));
            keywords.Add("keys", new Tuple<string>("keys Keyword"));
            keywords.Add("values", new Tuple<string>("values Keyword"));

            keywords.Add("if", new Tuple<string>("if Keyword"));
            keywords.Add("else", new Tuple<string>("else Keyword"));
            keywords.Add("for", new Tuple<string>("for Keyword"));
            keywords.Add("foreach", new Tuple<string>("foreach Keyword"));
            keywords.Add("while", new Tuple<string>("while Keyword"));
            keywords.Add("break", new Tuple<string>("break Keyword"));
            keywords.Add("continue", new Tuple<string>("continue Keyword"));
            keywords.Add("return", new Tuple<string>("return Keyword"));

            keywords.Add("create", new Tuple<string>("create Keyword"));
            keywords.Add("send", new Tuple<string>("send Keyword"));
            keywords.Add("raise", new Tuple<string>("raise Keyword"));
            keywords.Add("delete", new Tuple<string>("delete Keyword"));
            keywords.Add("assert", new Tuple<string>("assert Keyword"));
            keywords.Add("payload", new Tuple<string>("payload Keyword"));

            keywords.Add("var", new Tuple<string>("var Keyword"));
            keywords.Add("int", new Tuple<string>("int Keyword"));
            keywords.Add("bool", new Tuple<string>("bool Keyword"));
            keywords.Add("foreign", new Tuple<string>("foreign Keyword"));
            keywords.Add("any", new Tuple<string>("any Keyword"));
            keywords.Add("seq", new Tuple<string>("seq Keyword"));
            keywords.Add("map", new Tuple<string>("map Keyword"));

            return keywords;
        }
    }
}
