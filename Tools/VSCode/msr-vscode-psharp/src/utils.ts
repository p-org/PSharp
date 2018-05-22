/*---------------------------------------------------------
 * Copyright (C) Microsoft Corporation. All rights reserved.
 *--------------------------------------------------------*/

import vscode = require('vscode');
import { errorDiagnosticCollection, warningDiagnosticCollection } from './main';

export const tokenTips: Map<string, string> = new Map<string, string>([
    ["machine", "An abstract class representing a P# state machine."],
    ["MachineId", "A unique reference to a P# state machine class instance"],
    ["monitor", "An abstract class representing a P# monitor."],
    ["state", "A state in a P# state machine."],
    ["group", "A group of states or of other state groups in a P# state machine."],
    ["event", "An event that will be sent from one machine to another machine or raised to itself, usually triggering a state transition"],
    ["start", "The initial state for this P# state machine"],
    ["hot", "A liveness monitor state indicating that an operation is required and has not yet occurred"],
    ["cold", "A liveness monitor state indicating that an operation required by a hot state has been completed"],
    //["TokenType.EventIdentifier", "An event that will be sent from one machine to another machine or raised to itself"],
    //["TokenType.MachineIdentifier", "A P# state machine class definition"],
    //["TokenType.StateIdentifier", "A state in a P# state machine"],
    //["TokenType.StateGroupIdentifier", "A group of states or of other state groups in a P# state machine"],
    //["TokenType.ActionIdentifier", "An action to be performed"],
    //["TokenType.TypeIdentifier", "A type in P# code"],   // TODO better definition here
    //["TokenType.CreateMachine", "Create an instance of a P# state machine class"],
    //["TokenType.CreateRemoteMachine", "Create a remote instance of a P# state machine class"],
    ["send", "Send an event from one machine to another"],
    ["raise", "Send an event from this machine to itself"],
    ["jump", "Transition this machine to another state at the end of the current handler"],
    ["assert", "Assert that a condition is true"],
    ["assume", "Assume that a condition is true"],
    ["pop", "Pop a state from the state queue"],
    ["on", "Specify an event for which an action is to be performed"],
    ["do", "Specify an action to be performed when an event occurs"],
    ["goto", "Transition this machine to another state"],
    ["push", "Push a state onto the state queue"],
    ["with", "Perform an additional action on a state transition"],
    ["defer", "Defer handling of an event until the machine transitions out of the specified state"],
    ["ignore", "Ignore an event while the machine is in the specified state"],
    ["entry", "An action to be executed by a machine on entry to a state"],
    ["exit", "An action to be executed by a machine on exit from a state"],
    ["trigger", "A reference to the currently received event; may be cast to a specific event type to obtain the event payload, if any"],
    ["halt", "Halt the machine; consumes but does not operate on events"],
    ["default", "An action to be run when there is no event in the queue for this machine state"]
    //["TokenType.NonDeterministic", "Return a reproducibly random boolean value"]
]);

export function isPositionInQuotedString(lineText: string, position: vscode.Position): boolean {
	let lineTillCurrentPosition = lineText.substr(0, position.character);

	// Count the number of double quotes in the line till current position. Ignore escaped double quotes
	let doubleQuotesCnt = (lineTillCurrentPosition.match(/\"/g) || []).length;
	let escapedDoubleQuotesCnt = (lineTillCurrentPosition.match(/\\\"/g) || []).length;

	doubleQuotesCnt -= escapedDoubleQuotesCnt;
	return doubleQuotesCnt % 2 === 1;
}

