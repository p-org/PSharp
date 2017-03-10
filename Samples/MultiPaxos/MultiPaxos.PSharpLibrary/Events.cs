using Microsoft.PSharp;

namespace MultiPaxos.PSharpLibrary
{
    #region Events
    
    class local : Event { }
    class success : Event { }
    class goPropose : Event { }
    class response : Event { }

    #endregion
}
