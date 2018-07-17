Explicit termination of a machine
=================================
In order to terminate a P# machine explicitly, it must dequeue a special event named `halt`, which is provided by P# (the user cannot declare it).

A `halt` event (`Halt` when using P# as a library) can be raised and/or sent to another machine. Termination of a machine due to an unhandled `halt` event is valid behavior (the P# runtime does not report an error). From the point of view of formal operational semantics, a halted machine is fully receptive and consumes any event that is sent to it. The P# runtime implements this semantics efficiently by cleaning up resources allocated to a halted machine and recording that the machine has halted.

An event sent to a halted machine is simply dropped.

A halted machine cannot be restarted; it remains halted forever.
