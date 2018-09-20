using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PSharp.PSharpStateMachineStructureViewer
{
    /* Abstract base exception */
    public abstract class StateMachineStructureViewerBaseException : Exception
    {
        public readonly string token, context;
        public StateMachineStructureViewerBaseException(string msg, string token, string context) :
            base(msg)
        {
            this.token = token;
            this.context = context;
        }
    }

    /* Specific exception types */

    public class StateDiagramViewerException : StateMachineStructureViewerBaseException
    {
        public StateDiagramViewerException(string msg, string token, string context)
            : base(msg, token, context) { }
    }

    public class StateDiagramViewerUnresolvedTokenException : StateMachineStructureViewerBaseException
    {
        public StateDiagramViewerUnresolvedTokenException(string msg, string token, string context) 
            : base( msg, token, context) { }
    }

    public class StateDiagramViewerDuplicateTokenException : StateMachineStructureViewerBaseException
    {
        public StateDiagramViewerDuplicateTokenException(string msg, string token, string context)
            : base(msg, token, context) { }
    }
}
