
declare var d3: any;
declare var $: any;

type Log = { From: string, To: string, Message: string, MachineStates: { [key: string]: string } };

enum DisplayMode { Simulation, Timeline };

class JsonTraceParserResult {
    public Machines: string[];
    public Logs: Log[];

    public constructor(machines: string[], logs: Log[]) {
        this.Machines = machines;
        this.Logs = logs;
    }
}

class JsonTraceParser {
    public static Parse(trace: string): JsonTraceParserResult {
        var parsedTraces = JSON.parse(trace);
        if (!(parsedTraces instanceof Array)) {
            throw new Error("Invalid trace file - expected it to be an array of objects.");
        }

        var machines: string[] = [];
        for (var parsedTrace of parsedTraces) {
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
        for (var machineName of machines) {
            currentState[machineName] = "<i>No state info</i>";
        }

        var logs: Log[] = [];
        for (var parsedTrace of parsedTraces) {
            var from = parsedTrace.From;
            var to = parsedTrace.To;
            var message = parsedTrace.Message;
            var state: { [key: string]: string } = parsedTrace.State || {};

            for (let machineName in state) {
                currentState[machineName] = state[machineName];
            }

            var machineStates = {};
            for (var machineName of machines) {
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
    }
}

class VisualizerState {

    public DisplayMode: DisplayMode = DisplayMode.Timeline;

    public SimulationLogLength: number = 0;

    public CurrentLogIndex: number = -1;

    public Machines: string[] = [];

    public Logs: Log[] = [];

    public UpdateJsonTrace(jsonTrace: string) {

        var parserResult = JsonTraceParser.Parse(jsonTrace);
        this.Machines = parserResult.Machines;
        this.Logs = parserResult.Logs;
        this.SimulationLogLength = 0;
        this.CurrentLogIndex = this.DisplayMode == DisplayMode.Timeline ?
            this.Logs.length - 1 :
            -1;
       
        var machineOrder = $("#machine-order");
        machineOrder.empty();

        machineOrder.append("<span>Machine order: </span>")
        for (var machineName of visualizerState.Machines) {
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
    }

    public MoveLogForward() {
        if (this.DisplayMode == DisplayMode.Simulation &&
            this.SimulationLogLength < this.Logs.length) {
            this.SimulationLogLength++;
            this.CurrentLogIndex = this.SimulationLogLength - 1;
        }
    }

    public MoveLogBackward() {
        if (this.DisplayMode == DisplayMode.Simulation &&
            this.SimulationLogLength > 0) {
            this.SimulationLogLength--;
            this.CurrentLogIndex = this.SimulationLogLength - 1;
        }
    }
}

class Visualizer {

    // The horizontal distance between the swim lane for each machine
    private static MachineSeparation: number = 100;

    // The distance left between the first and last event and the beginning
    // and end of the swim lane
    private static SwimLaneTopBottomMargin: number = 20;

    // The vertical distance occupied between a message send and receive
    private static EventSpan: number = 20;

    // The vertical distance between different message sends and receives
    private static EventSeparation: number = 40;

    // The radius of each circle denoting a message send or receive
    private static EventRadius: number = 3;

    // The sapce between the start of the swim lane and the 
    // machine label
    private static MachineLabelSwimLaneGap: number = 10;

    // The amount of vertical space to allocate to the machine
    // labels (they will flow into the space occupied MarginTop
    // if they exceed this space)
    private static MachineLabelVerticalLength: number = 10;

    // The margins between the swim lanes and the svg container
    private static Margin = {
        Left: 100,
        Right: 100,
        Top: 30,
        Bottom: 30
    };

    public static Draw(visualizerState: VisualizerState) {
        Visualizer.DrawSequenceDiagram(visualizerState);
        Visualizer.DisplayStates(visualizerState);
    }

    public static DrawSequenceDiagram(visualizerState: VisualizerState) {

        var svg = d3.select("#diagram");

        svg.selectAll("*").remove();

        var swimLaneVerticalLength =
            2 * Visualizer.SwimLaneTopBottomMargin +
            Visualizer.EventSpan +  // The space occupied by the first event
            (visualizerState.Logs.length - 1) * (Visualizer.EventSpan + Visualizer.EventSeparation); // and the rest (along with space between events)

        var machineLabelTop = Visualizer.Margin.Top;
        var machineLabelBottom = machineLabelTop + Visualizer.MachineLabelVerticalLength;
        var swimLaneTop = machineLabelBottom + Visualizer.MachineLabelSwimLaneGap;
        var swimLaneBottom = swimLaneTop + swimLaneVerticalLength;

        var xSpan = visualizerState.Machines.length * Visualizer.MachineSeparation;
        var ySpan =
            Visualizer.MachineLabelVerticalLength +
            Visualizer.MachineLabelSwimLaneGap +
            swimLaneVerticalLength;

        svg.attr("width", Visualizer.Margin.Left + xSpan + Visualizer.Margin.Right);
        svg.attr("height", Visualizer.Margin.Top + ySpan + Visualizer.Margin.Bottom);

        var machineXCoordinate: { string: number } = <{ string: number }>{};

        for (var i = 0; i < visualizerState.Machines.length; i++) {
            var x = Visualizer.Margin.Left + (i * Visualizer.MachineSeparation);

            var machineName = visualizerState.Machines[i];

            svg.append("text")
                .attr("x", x)
                .attr("y", machineLabelBottom)
                .attr("transform", "rotate(-45)")
                .attr("transform-origin", x + " " + machineLabelBottom)
                .text(function (d) { return machineName; });

            var line = svg.append("line");
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

        visualizerState.Logs.slice(0, maxLogLength).forEach(function (log, i) {
            var startX = machineXCoordinate[log.From];
            var endX = machineXCoordinate[log.To];
            var startY = swimLaneTop + Visualizer.SwimLaneTopBottomMargin + i * (Visualizer.EventSeparation + Visualizer.EventSpan);
            var endY = startY + Visualizer.EventSpan;

            var fromMachineCircle = svg.append("circle");
            self.ApplyAttributes(fromMachineCircle, {
                "cx": startX,
                "cy": startY,
                "r": Visualizer.EventRadius,
                "fill": "gray"
            });

            var toMachineCircle = svg.append("circle");
            self.ApplyAttributes(toMachineCircle, {
                "cx": endX,
                "cy": endY,
                "r": Visualizer.EventRadius,
                "fill": "gray"
            });

            var startingPoint = self.pointOnEdgeOfCircleTowards(
                startX,
                startY,
                Visualizer.EventRadius,
                endX,
                endY);

            var endingPoint = self.pointOnEdgeOfCircleTowards(
                endX,
                endY,
                Visualizer.EventRadius,
                startX,
                startY);

            var messageLine = svg.append("line");
            self.ApplyAttributes(messageLine, {
                "class": "messageLine",
                "x1": startingPoint.x,
                "y1": startingPoint.y,
                "x2": endingPoint.x,
                "y2": endingPoint.y,
                "stroke-width": 1,
                "stroke": "gray",
                "log-index": i
            });

            svg.append("text")
                .attr("x", startX + ((endX - startX) * 0.5))
                .attr("y", startY + ((endY - startY) * 0.5))
                .style("fill", "gray")
                .text(function (d) { return log.Message; });
        });
    }

    public static DisplayStates(visualizerState: VisualizerState) {
        var machineStatesContainer = $("#machine-states");
        machineStatesContainer.empty();

        var logIndex = visualizerState.CurrentLogIndex;

        if (logIndex < 0) {
            return;
        }

        var machineStates = visualizerState.Logs[logIndex].MachineStates;

        for (var machineName of visualizerState.Machines) {
            var stateContainer = machineStatesContainer.append("<div class=\"machine-state-container\"></div>");
            stateContainer.append("<div class=\"machine-name\">" + machineName + "</div>");
            stateContainer.append("<div class=\"machine-state\">" + machineStates[machineName] + "</div>");
        }
    }

    private static ApplyAttributes(svgElement: any, attributes: { [key: string]: any }) {
        Object.keys(attributes).forEach(function (k) {
            svgElement.attr(k, attributes[k]);
        });
    }

    private static pointOnEdgeOfCircleTowards(
        circleX: number,
        circleY: number,
        radius: number,
        targetX: number,
        targetY: number) {
        var directionX = targetX - circleX;
        var directionY = targetY - circleY;

        var sqrtOfDxAndDySquared = Math.sqrt(Math.pow(directionX, 2) + Math.pow(directionY, 2));

        return {
            x: circleX + directionX * (radius / sqrtOfDxAndDySquared),
            y: circleY + directionY * (radius / sqrtOfDxAndDySquared)
        };
    }
}

var visualizerState = new VisualizerState();

function UpdateVisualization() {
    Visualizer.Draw(visualizerState);
}

window.onload = () => {
    var filePickerInput = document.getElementById("jsonFilePicker");
    var filePickerButton = document.getElementById("jsonFilePickerButton");
    var simulationCheckbox = document.getElementById("simulationCheckbox");

    filePickerButton.addEventListener("click", function (e) {
        filePickerInput.click();
    }, false);

    filePickerInput.onchange = function (this: HTMLElement, e: Event): any {
        var event: any = e;
        var files = event.target.files;
        var file = files[0];
        var reader = new FileReader();
        var inputControl: HTMLInputElement = <HTMLInputElement>this;
        reader.onload = function (e: any) {
            var jsonTrace = e.target.result;
            visualizerState.UpdateJsonTrace(jsonTrace);
            inputControl.value = null;
            UpdateVisualization();
        };
        reader.readAsText(file);
    };

    simulationCheckbox.onchange = function (this: HTMLElement, e: Event): any {
        var event: any = e;

        visualizerState.DisplayMode = event.target.checked ?
            DisplayMode.Simulation :
            DisplayMode.Timeline;

        if (visualizerState.DisplayMode == DisplayMode.Timeline) {
            visualizerState.CurrentLogIndex = visualizerState.Logs.length - 1;
        }

        UpdateVisualization();
    }

    document.onkeydown = function (e: Event) {
        var event: any = e;

        if (event.key == "ArrowLeft") {
            visualizerState.MoveLogBackward();
            UpdateVisualization();
        }
        else if (event.key == "ArrowRight") {
            visualizerState.MoveLogForward();
            UpdateVisualization();
        }

        return true;
    }

    document.getElementById("diagram").addEventListener('click', function (e: Event) {
        var event: any = e;
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
