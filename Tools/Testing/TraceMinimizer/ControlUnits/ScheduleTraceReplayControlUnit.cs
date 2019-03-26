using Microsoft.PSharp.TestingServices.Tracing.Schedule;
using Microsoft.PSharp.TestingServices.Tracing.TreeTrace;
using System;
using System.Collections.Generic;
using System.Text;

namespace PSharpMinimizer.ControlUnits
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

        ScheduleTraceReplayControlUnit(ScheduleTrace schedTrace)
        {
            BestTree = null;
            ScheduleTrace = schedTrace;
        }

        public bool PrepareForNextIteration(EventTree resultTree)
        {
            Valid = resultTree.reproducesBug();
            Completed = true;
            return Completed;
        }
    }
}
