using Microsoft.PSharp.TestingServices.Tracing.TreeTrace;
using System;
using System.Collections.Generic;
using System.Text;

namespace PSharpMinimizer.ControlUnits
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
        bool PrepareForNextIteration(EventTree resultTree);

        bool Valid { get; }
        bool Completed { get; }
    }
}
