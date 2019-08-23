// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;


namespace Microsoft.PSharp.VisualStudio
{
    internal static class Keywords
    {
        /// <summary>
        /// The P# keywords.
        /// </summary>
        public static Dictionary<string, string> DefinitionMap = new Dictionary<string, string>
        {
            ["private"] = "private Keyword",
            ["protected"] = "protected Keyword",
            ["internal"] = "internal Keyword",
            ["public"] = "public Keyword",
            ["abstract"] = "abstract Keyword",
            ["virtual"] = "virtual Keyword",
            ["override"] = "override Keyword",

            ["namespace"] = "namespace Keyword",
            ["using"] = "using Keyword",

            ["main"] = "main Keyword",
            ["start"] = "start Keyword",
            ["extern"] = "extern Keyword",

            ["machine"] = "machine Keyword",
            ["model"] = "model Keyword",
            ["monitor"] = "monitor Keyword",
            ["state"] = "state Keyword",
            ["event"] = "event Keyword",
            ["action"] = "action Keyword",
            ["fun"] = "fun Keyword",

            ["on"] = "on Keyword",
            ["do"] = "do Keyword",
            ["goto"] = "goto Keyword",
            ["defer"] = "defer Keyword",
            ["ignore"] = "ignore Keyword",
            ["to"] = "to Keyword",
            ["entry"] = "entry Keyword",
            ["exit"] = "exit Keyword",

            ["this"] = "this Keyword",
            ["base"] = "base Keyword",
            ["new"] = "new Keyword",
            ["null"] = "null Keyword",
            ["true"] = "true Keyword",
            ["false"] = "false Keyword",

            ["sizeof"] = "sizeof Keyword",
            ["in"] = "in Keyword",
            ["as"] = "as Keyword",
            ["keys"] = "keys Keyword",
            ["values"] = "values Keyword",

            ["if"] = "if Keyword",
            ["else"] = "else Keyword",
            ["for"] = "for Keyword",
            ["foreach"] = "foreach Keyword",
            ["while"] = "while Keyword",
            ["break"] = "break Keyword",
            ["continue"] = "continue Keyword",
            ["return"] = "return Keyword",

            ["create"] = "create Keyword",
            ["send"] = "send Keyword",
            ["raise"] = "raise Keyword",
            ["delete"] = "delete Keyword",
            ["assert"] = "assert Keyword",
            ["payload"] = "payload Keyword",

            ["var"] = "var Keyword",
            ["int"] = "int Keyword",
            ["bool"] = "bool Keyword",
            ["foreign"] = "foreign Keyword",
            ["any"] = "any Keyword",
            ["seq"] = "seq Keyword",
            ["map"] = "map Keyword",
        };
    }
}
