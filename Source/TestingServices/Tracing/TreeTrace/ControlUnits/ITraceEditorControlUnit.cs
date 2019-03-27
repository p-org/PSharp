using Microsoft.PSharp.TestingServices.Tracing.TreeTrace;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PSharp.TestingServices.Tracing.TreeTrace.ControlUnits
{
    interface ITraceEditorControlUnit
    {

        TreeTraceEditor.TraceEditorMode RequiredTraceEditorMode { get; }

        EventTree BestTree { get; }
        /// <summary>
        /// Inclusive left bound
        /// </summary>
        int Left { get; }

        /// <summary>
        /// Inclusive right bound
        /// </summary>
        int Right { get;  }

        void PrepareForNextIteration(EventTree resultTree);

        bool Valid { get; }  // TODO: Revise what this means - Error or just that it did not reproduce the bug
        bool Completed { get; }
    }
}
