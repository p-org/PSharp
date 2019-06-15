var DisplayMode;
(function (DisplayMode) {
    DisplayMode[DisplayMode["Simulation"] = 0] = "Simulation";
    DisplayMode[DisplayMode["Timeline"] = 1] = "Timeline";
})(DisplayMode || (DisplayMode = {}));
;
var JsonTraceParserResult = /** @class */ (function () {
    function JsonTraceParserResult(machines, logs) {
        this.Machines = machines;
        this.Logs = logs;
    }
    return JsonTraceParserResult;
}());
var JsonTraceParser = /** @class */ (function () {
    function JsonTraceParser() {
    }
    JsonTraceParser.Parse = function (trace) {
        var parsedTraces = JSON.parse(trace);
        if (!(parsedTraces instanceof Array)) {
            throw new Error("Invalid trace file - expected it to be an array of objects.");
        }
        var machines = [];
        for (var _i = 0, parsedTraces_1 = parsedTraces; _i < parsedTraces_1.length; _i++) {
            var parsedTrace = parsedTraces_1[_i];
            var from = parsedTrace.From;
            var to = parsedTrace.To;
            if (from != "" && machines.indexOf(from) == -1) {
                machines.push(from);
            }
            if (to != "" && machines.indexOf(to) == -1) {
                machines.push(to);
            }
        }
        var currentState = {};
        for (var _a = 0, machines_1 = machines; _a < machines_1.length; _a++) {
            var machineName = machines_1[_a];
            currentState[machineName] = "<i>No state info</i>";
        }
        var logs = [];
        for (var _b = 0, parsedTraces_2 = parsedTraces; _b < parsedTraces_2.length; _b++) {
            var parsedTrace = parsedTraces_2[_b];
            var from = parsedTrace.From;
            var to = parsedTrace.To;
            var message = parsedTrace.Message;
            var state = parsedTrace.State || {};
            for (var machineName_1 in state) {
                currentState[machineName_1] = state[machineName_1];
            }
            var machineStates = {};
            for (var _c = 0, machines_2 = machines; _c < machines_2.length; _c++) {
                var machineName = machines_2[_c];
                machineStates[machineName] = currentState[machineName];
            }
            logs.push({
                From: from,
                To: to,
                Message: message,
                MachineStates: machineStates
            });
        }
        return new JsonTraceParserResult(machines, logs);
    };
    return JsonTraceParser;
}());
var VisualizerState = /** @class */ (function () {
    function VisualizerState() {
        this.DisplayMode = DisplayMode.Timeline;
        this.SimulationLogLength = 0;
        this.CurrentLogIndex = -1;
        this.Machines = [];
        this.Logs = [];
    }
    VisualizerState.prototype.UpdateJsonTrace = function (jsonTrace) {
        var parserResult = JsonTraceParser.Parse(jsonTrace);
        this.Machines = parserResult.Machines;
        this.Logs = parserResult.Logs;
        this.SimulationLogLength = 0;
        this.CurrentLogIndex = this.DisplayMode == DisplayMode.Timeline ?
            this.Logs.length - 1 :
            -1;
        var machineOrder = $("#machine-order");
        machineOrder.empty();
        machineOrder.append("<span>Machine order: </span>");
        for (var _i = 0, _a = visualizerState.Machines; _i < _a.length; _i++) {
            var machineName = _a[_i];
            machineOrder.append("<span class=\"orderable-machine\">" + machineName + "</span>");
        }
        var self = this;
        machineOrder.sortable({
            update: function () {
                var children = machineOrder.children(".orderable-machine");
                self.Machines = [];
                for (var i = 0; i < children.length; i++) {
                    self.Machines.push(children[i].innerText);
                }
                UpdateVisualization();
            }
        });
    };
    VisualizerState.prototype.MoveLogForward = function () {
        if (this.DisplayMode == DisplayMode.Simulation &&
            this.SimulationLogLength < this.Logs.length) {
            this.SimulationLogLength++;
            this.CurrentLogIndex = this.SimulationLogLength - 1;
        }
    };
    VisualizerState.prototype.MoveLogBackward = function () {
        if (this.DisplayMode == DisplayMode.Simulation &&
            this.SimulationLogLength > 0) {
            this.SimulationLogLength--;
            this.CurrentLogIndex = this.SimulationLogLength - 1;
        }
    };
    return VisualizerState;
}());
var Visualizer = /** @class */ (function () {
    function Visualizer() {
    }
    Visualizer.Draw = function (visualizerState) {
        Visualizer.DrawSequenceDiagram(visualizerState);
        Visualizer.DisplayStates(visualizerState);
    };
    Visualizer.DrawSequenceDiagram = function (visualizerState) {
        var diagramHeader = d3.select("#diagram-header");
        var sequenceDiagramContainer = document.getElementById("sequence-diagram-container");
        var svgDiagram = d3.select("#diagram");
        var svgSequenceDiagram = d3.select("#sequence-diagram");
        sequenceDiagramContainer.style.maxHeight = Visualizer.MaxDiagramHeight.toString() + "px";
        diagramHeader.selectAll("*").remove();
        svgSequenceDiagram.selectAll("*").remove();
        var swimLaneVerticalLength = 2 * Visualizer.SwimLaneTopBottomMargin +
            Visualizer.EventSpan + // The space occupied by the first event
            (visualizerState.Logs.length - 1) * (Visualizer.EventSpan + Visualizer.EventSeparation); // and the rest (along with space between events)
        var machineLabelTop = Visualizer.Margin.Top;
        var machineLabelBottom = machineLabelTop + Visualizer.MachineLabelVerticalLength;
        var swimLaneTop = Visualizer.MachineLabelSwimLaneGap;
        var swimLaneBottom = swimLaneTop + swimLaneVerticalLength;
        var xSpan = visualizerState.Machines.length * Visualizer.MachineSeparation;
        var ySpan = swimLaneVerticalLength;
        diagramHeader.attr("width", Visualizer.Margin.Left + xSpan + Visualizer.Margin.Right);
        diagramHeader.attr("height", Visualizer.Margin.Top + Visualizer.MachineLabelVerticalLength);
        svgDiagram.attr("width", Visualizer.Margin.Left + xSpan + Visualizer.Margin.Right);
        svgDiagram.attr("height", Visualizer.MachineLabelSwimLaneGap + ySpan + Visualizer.Margin.Bottom);
        var machineXCoordinate = {};
        for (var i = 0; i < visualizerState.Machines.length; i++) {
            var x = Visualizer.Margin.Left + (i * Visualizer.MachineSeparation);
            var machineName = visualizerState.Machines[i];
            diagramHeader.append("text")
                .attr("x", x)
                .attr("y", machineLabelBottom)
                .attr("transform", "rotate(-45)")
                .attr("transform-origin", x + " " + machineLabelBottom)
                .text(function (d) { return machineName; });
            var line = svgSequenceDiagram.append("line");
            this.ApplyAttributes(line, {
                "x1": x,
                "y1": swimLaneTop,
                "x2": x,
                "y2": swimLaneBottom,
                "stroke-width": 1,
                "stroke": "black",
                "stroke-dasharray": "5,5,5"
            });
            machineXCoordinate[machineName] = x;
        }
        var maxLogLength = visualizerState.DisplayMode == DisplayMode.Simulation ?
            visualizerState.SimulationLogLength :
            visualizerState.Logs.length;
        var self = this;
        var lastYPoint = 0;
        visualizerState.Logs.slice(0, maxLogLength).forEach(function (log, i) {
            var startX = machineXCoordinate[log.From];
            var endX = machineXCoordinate[log.To];
            var startY = swimLaneTop + Visualizer.SwimLaneTopBottomMargin + i * (Visualizer.EventSeparation + Visualizer.EventSpan);
            var endY = startY + Visualizer.EventSpan;
            var fromMachineCircle = svgSequenceDiagram.append("circle");
            self.ApplyAttributes(fromMachineCircle, {
                "cx": startX,
                "cy": startY,
                "r": Visualizer.EventRadius,
                "fill": "gray"
            });
            var toMachineCircle = svgSequenceDiagram.append("circle");
            self.ApplyAttributes(toMachineCircle, {
                "cx": endX,
                "cy": endY,
                "r": Visualizer.EventRadius,
                "fill": "gray"
            });
            var startingPoint = self.pointOnEdgeOfCircleTowards(startX, startY, Visualizer.EventRadius, endX, endY);
            var endingPoint = self.pointOnEdgeOfCircleTowards(endX, endY, Visualizer.EventRadius, startX, startY);
            var messageLine = svgSequenceDiagram.append("line");
            self.ApplyAttributes(messageLine, {
                "class": "messageLine",
                "x1": startingPoint.x,
                "y1": startingPoint.y,
                "x2": endingPoint.x,
                "y2": endingPoint.y,
                "stroke-width": 1,
                "stroke": "gray",
                "log-index": i,
                "marker-end": "url(#arrow)"
            });
            var isArrowGoingLeft = endingPoint.x - startingPoint.x >= 0;
            var placementDelta = isArrowGoingLeft ? 0.2 : 0.8;
            svgSequenceDiagram.append("text")
                .attr("x", startX + ((endX - startX) * placementDelta))
                .attr("y", startY + ((endY - startY) * placementDelta))
                .style("fill", "gray")
                .text(function (d) { return log.Message; });
            lastYPoint = endingPoint.y;
        });
        var targetYScroll = lastYPoint - (Visualizer.MaxDiagramHeight / 2);
        targetYScroll = targetYScroll < 0 ? 0 : targetYScroll;
        sequenceDiagramContainer.scrollTo(0, targetYScroll);
    };
    Visualizer.DisplayStates = function (visualizerState) {
        var machineStatesContainer = $("#machine-states");
        machineStatesContainer.empty();
        var logIndex = visualizerState.CurrentLogIndex;
        if (logIndex < 0) {
            return;
        }
        var machineStates = visualizerState.Logs[logIndex].MachineStates;
        for (var _i = 0, _a = visualizerState.Machines; _i < _a.length; _i++) {
            var machineName = _a[_i];
            var stateContainer = machineStatesContainer.append("<div class=\"machine-state-container\"></div>");
            stateContainer.append("<div class=\"machine-name\">" + machineName + "</div>");
            stateContainer.append("<div class=\"machine-state\">" + machineStates[machineName] + "</div>");
        }
    };
    Visualizer.ApplyAttributes = function (svgElement, attributes) {
        Object.keys(attributes).forEach(function (k) {
            svgElement.attr(k, attributes[k]);
        });
    };
    Visualizer.pointOnEdgeOfCircleTowards = function (circleX, circleY, radius, targetX, targetY) {
        var directionX = targetX - circleX;
        var directionY = targetY - circleY;
        var sqrtOfDxAndDySquared = Math.sqrt(Math.pow(directionX, 2) + Math.pow(directionY, 2));
        return {
            x: circleX + directionX * (radius / sqrtOfDxAndDySquared),
            y: circleY + directionY * (radius / sqrtOfDxAndDySquared)
        };
    };
    // The horizontal distance between the swim lane for each machine
    Visualizer.MachineSeparation = 100;
    // The distance left between the first and last event and the beginning
    // and end of the swim lane
    Visualizer.SwimLaneTopBottomMargin = 20;
    // The vertical distance occupied between a message send and receive
    Visualizer.EventSpan = 20;
    // The vertical distance between different message sends and receives
    Visualizer.EventSeparation = 40;
    // The radius of each circle denoting a message send or receive
    Visualizer.EventRadius = 3;
    // The space between the start of the swim lane and the
    // machine label
    Visualizer.MachineLabelSwimLaneGap = 10;
    // The amount of vertical space to allocate to the machine
    // labels (they will flow into the space occupied MarginTop
    // if they exceed this space)
    Visualizer.MachineLabelVerticalLength = 10;
    // The max heigh of the svg sequence diagram in pixels.
    Visualizer.MaxDiagramHeight = 500;
    // The margins between the swim lanes and the svg container
    Visualizer.Margin = {
        Left: 10,
        Right: 10,
        Top: 30,
        Bottom: 30
    };
    return Visualizer;
}());
var visualizerState = new VisualizerState();
function UpdateVisualization() {
    Visualizer.Draw(visualizerState);
}
window.onload = function () {
    var filePickerInput = document.getElementById("jsonFilePicker");
    var filePickerButton = document.getElementById("jsonFilePickerButton");
    var simulationCheckbox = document.getElementById("simulationCheckbox");
    filePickerButton.addEventListener("click", function (e) {
        filePickerInput.click();
    }, false);
    filePickerInput.onchange = function (e) {
        var event = e;
        var files = event.target.files;
        var file = files[0];
        var reader = new FileReader();
        var inputControl = this;
        reader.onload = function (e) {
            var jsonTrace = e.target.result;
            visualizerState.UpdateJsonTrace(jsonTrace);
            inputControl.value = null;
            UpdateVisualization();
        };
        reader.readAsText(file);
    };
    simulationCheckbox.onchange = function (e) {
        var event = e;
        visualizerState.DisplayMode = event.target.checked ?
            DisplayMode.Simulation :
            DisplayMode.Timeline;
        if (visualizerState.DisplayMode == DisplayMode.Timeline) {
            visualizerState.CurrentLogIndex = visualizerState.Logs.length - 1;
        }
        UpdateVisualization();
    };
    document.onkeydown = function (e) {
        var event = e;
        if (event.key == "ArrowLeft") {
            visualizerState.MoveLogBackward();
            UpdateVisualization();
        }
        else if (event.key == "ArrowRight") {
            visualizerState.MoveLogForward();
            UpdateVisualization();
        }
        return false;
    };
    document.getElementById("diagram").addEventListener('click', function (e) {
        var event = e;
        var target = event.target;
        if (target.className &&
            target.className.baseVal &&
            target.className.baseVal.indexOf("messageLine") != -1) {
            var logIndex = parseInt(target.getAttribute("log-index"), 10);
            visualizerState.CurrentLogIndex = logIndex;
            UpdateVisualization();
        }
    });
};
//# sourceMappingURL=app.js.map