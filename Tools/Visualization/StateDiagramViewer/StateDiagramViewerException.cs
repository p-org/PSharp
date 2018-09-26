using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PSharp.StateDiagramViewer
{
    /* Abstract base exception */
    public abstract class StateDiagramViewerBaseException : Exception
    {
        public readonly string token, context;
        public StateDiagramViewerBaseException(string msg, string token, string context) :
            base(msg)
        {
            this.token = token;
            this.context = context;
        }
    }

    /* Specific exception types */

    public class StateDiagramViewerException : StateDiagramViewerBaseException
    {
        public StateDiagramViewerException(string msg, string token, string context)
            : base(msg, token, context) { }
    }

    public class StateDiagramViewerUnresolvedTokenException : StateDiagramViewerBaseException
    {
        public StateDiagramViewerUnresolvedTokenException(string msg, string token, string context) 
            : base( msg, token, context) { }
    }

    public class StateDiagramViewerDuplicateTokenException : StateDiagramViewerBaseException
    {
        public StateDiagramViewerDuplicateTokenException(string msg, string token, string context)
            : base(msg, token, context) { }
    }
}
