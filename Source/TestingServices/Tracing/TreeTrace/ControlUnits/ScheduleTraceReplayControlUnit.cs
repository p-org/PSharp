using Microsoft.PSharp.TestingServices.Tracing.Schedule;
using Microsoft.PSharp.TestingServices.Tracing.TreeTrace;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PSharp.TestingServices.Tracing.TreeTrace.ControlUnits
{
    class ScheduleTraceReplayControlUnit : ITraceEditorControlUnit
    {
        public TreeTraceEditor.TraceEditorMode RequiredTraceEditorMode { get { return TreeTraceEditor.TraceEditorMode.ScheduleTraceReplay; } }
        public EventTree BestTree { get; private set; }
        public ScheduleTrace ScheduleTrace { get; }
        public int Left { get; private set; }
        public int Right { get; private set; }

        public bool Valid { get; private set; }

        public bool Completed { get; private set; }

        public ScheduleTraceReplayControlUnit(ScheduleTrace schedTrace)
        {
            BestTree = null;
            ScheduleTrace = schedTrace;
            Valid = true;
            Completed = false;
        }

        public bool PrepareForNextIteration(EventTree resultTree)
        {
            if (resultTree.reproducesBug())
            {
                BestTree = resultTree;
            }
            Completed = true;
            Valid = resultTree.reproducesBug();
            return Completed && Valid;
        }

    }
}
