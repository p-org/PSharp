Testing P# programs and reproducing bugs
========================================
The P# tester can be used to **systematically test** a P# program to find **safety** and **liveness property violations** (specified by the user).

To invoke the tester use the following command:
```
.\PSharpTester.exe /test:${PSHARP_PROGRAM}.exe
```
Where `${PSHARP_PROGRAM}` is the path to a P# executable (or library) that contains a method annotated with the `[Microsoft.PSharp.Test]` attribute. This method is the entry point to the test. Read more [here](#test-entry-point).

## Controlled, serialized and reproducible testing
In its essence, the P# tester works as follows: it (1) **serializes** the execution of an asynchronous P# program, (2) **takes control** of the underlying machine/task scheduler and any declared sources of non-determinism in the program (e.g. timers), and (3) **explores** scheduling decisions and non-deterministic choices to trigger bugs.

Because of the above capabilities, the P# tester is capable to quickly discover bugs that would be very hard to discover using traditional testing techniques.

During testing, the P# tester executes a program from start to finish for a user-specified number of testing iterations. During each iteration, the tester is exploring a potentially different serialized execution path. If a bug is discovered, the tester will terminate and dump a reproducible trace (including a human readable version of it).

Read [here](#reproducing-and-debugging-traces) to learn how to reproduce traces and debug them using the Visual Studio IDE.

## Test entry point
A P# test method can be declared as follows:
```c#
[Microsoft.PSharp.Test]
public static void Execute(IMachineRuntime runtime)
{
  runtime.RegisterMonitor(typeof(SomeMonitor));
  runtime.CreateMachine(typeof(SomeMachine));
}
```
This method acts as the entry point to each testing iteration. Note that the P# tester will internally create a special `TestHarnessMachine` P# machine (not exposed to the user), which invokes the test method and executes it. This allows us to capture and report any errors that occur outside the scope of a user machine (e.g. before the very first machine is created).

Note that similar to unit-testing, static state should be appropriately reset during testing with the P# tester, since iterations run in shared memory. However, [parallel instances of the tester](#parallel-and-portfolio-testing) run in separate processes, which provides isolation.

## Testing options
To see the **list of available command line options** use the flag `/?`. Note that there are many other (experimental) options, which are not shown yet in the help menu (e.g. because they are considered unstable or non-mainstream).

You can optionally give the **number of testing iterations** to perform using `/i:N` (N > 1). If this flag is not provided, the tester will perform 1 iteration by default.

You can also provide a **timeout**, by providing the flag `/timeout:N` (N > 0, with N specifying how many seconds before the timeout). If no iterations are specified (thus the default number of iterations is used), then the tester will perform testing iterations until the timeout is reached.

## Parallel and portfolio testing
The P# tester supports **parallel** and **portfolio** testing.

To enable parallel testing, you must run `PSharpTester.exe`, and provide the flag `/parallel:N` (N > 1, with N specifying the number of parallel testing processes to be spawned). By default, the tester spawns the same testing process multiple times (using different random seeds).

To enable portfolio testing, you must run `PSharpTester.exe` in parallel (as specified above), and also provide the flag `/sch:portfolio`. Portfolio testing currently spawns N (depending on `/parallel:N`) testing processes that use a collection of randomized exploration strategies (including fuzzing with different seeds, and probabilistic prioritized exploration).

## Reproducing and debugging traces
The P# replayer can be used to deterministically reproduce and debug buggy executions (found by `PSharpTester.exe`). To run the replayer use the following command:
```
.\PSharpReplayer.exe /test:${PSHARP_PROGRAM}.exe /replay:${SCHEDULE_TRACE}.schedule
```
Where `${SCHEDULE_TRACE}}.schedule` is the trace dumped by `PSharpTester.exe`.

You can attach the Visual Studio debugger on this trace, to get the familiar VS debugging experience, by using `/break`. When using this flag, P# will automatically instrument a breakpoint when the bug is found. You can also insert your own breakpoints in the source code as usual.

To see the available command line options use the option `/?`.
