Effectively checking liveness properties
========================================
The presence of Monitors that have `hot` and `cold` states implicitly specifies two assertions. We will refer to monitor states that are marked neither `hot` nor `cold` as `warm` states. First, any terminated execution of the program must not have a monitor in a hot state. Second, the program should not have infinite executions that remain in hot (or warm) states infinitely often without transitioning to a cold state. 

While the former is a safety property and easily checked, the latter requires generation of infinite executions, which is not really possible in practice and we must resort to heuristics. P# maintains a _temperature_ for each monitor. The temperature goes up by a unit if the monitor transitions to a hot state, it goes to zero on a transition to a cold state and stays the same on transition to a warm state. The P# tester looks for executions where the temperature of a monitor exceeds a particular large threshold because it indicates a long suffix stuck in hot/warm states without transitioning to a cold state. The definition of what is _large_ is where the user must be involved. 

The tester accepts a flag `/max-steps:N`. Using this flag, the user can say that the program is expected to execute around N steps. Executions substantially longer than N are treated as potential infinite executions. But what is a step and how does one estimate N? This happens using a few iterations of the tester. For example, consider the sample program in `Samples\PSharpAsLibrary\Raft`. Let us compile and test it as follows, where we choose to set N as 200:
```
PSharpCompiler.exe /s:Samples.sln /p:Raft /t:test
PSharpTester.exe /test:Binaries\Debug\Raft.dll /i:10 /parallel:4 /sch:portfolio /max-steps:200
```

The tester will produce output, ending with something like the following:
```
... Found 0 bugs.
... Scheduling statistics:
..... Explored 40 schedules: 20 fair and 20 unfair.
..... Number of scheduling points (in fair terminating schedules): 78 (min), 352 (avg), 726 (max).
..... Exceeded the max-steps bound of '200' in 85.00% of the fair schedules.
..... Hit the max-steps bound of '200' in 60.00% of the unfair schedules.
... Elapsed 11.4442111 sec.
. Done
```

Note the line that reads `Exceeded the max-steps bound of '200' in 85.00% of the fair schedules`. It means that the program execution exceeded 200 steps several times (85% of the times) to reach termination. The line above it indicates that execution lengths ranged from 78 steps to 726 steps, averaging 352 steps. Going by this output, we decide to  increase the bound to 1000, re-run the tester and this time get an output like:
```
... Found 0 bugs.
... Scheduling statistics:
..... Explored 40 schedules: 20 fair and 20 unfair.
..... Number of scheduling points (in fair terminating schedules): 79 (min), 317 (avg), 966 (max).
..... Exceeded the max-steps bound of '1000' in 20.00% of the fair schedules.
..... Hit the max-steps bound of '1000' in 5.00% of the unfair schedules.
... Elapsed 16.4418592 sec.
. Done
```

The testing is a little bit slower: taking 16 seconds for the same number of iterations. But we hit the bound much fewer times, making the testing much more effective as it more often covers the entire length of program execution. In general, it is not necessary to make this percentage go to zero. Often times, programs can exceed their expected length of execution, either because of bugs, or because of corner-case scheduling that delays important events more than usual. One can argue picking a max-steps bound that is very large. However, this will considerably slow down the tester (when using unfair schedulers; see the next section for details). Thus, we anticipate the user doing a few iterations with the tester before settling down on the desired max-steps bound.

To understand the details behind _fair_ and _unfair_ scheduling that is mentioned in the output above, we move on to next section targeted towards more advanced usage of P#. 

## Fair and Unfair Scheduling
We refer interested readers to the following technical paper that explains the concept behind fair and unfair scheduling:

**Fair stateless model checking. Madan Musuvathi and Shaz Qadeer. PLDI 2008. [PDF](https://www.microsoft.com/en-us/research/publication/fair-stateless-model-checking/)**

Consider a program with two machines A and B. The machine A continuously sends an event to itself until it receives a message from B. The machine B is ready to send the message to A immediately upon creation. (Contrast this example to Figure 3 of the paper.) This program has an infinite execution: where A is continuously scheduled without giving B a chance. Such an infinite execution is called _unfair_ because B is starved over an infinitely long period of time, which is unrealistic in modern systems.

The P# tester works by taking over the scheduling of the P# program. It uses one of several _schedulers_: algorithms that decide which machine to schedule next. A scheduler is called _fair_ if it is not expected to generate unfair executions. For example, the random scheduler, which makes decisions on the next machine to schedule randomly, is fair. In the program described above, it is very likely that B will be given a chance to execute. Some schedulers don't have this property and are called _unfair_ schedulers. Unfair schedulers have a role to play in finding violations of safety properties, but not in finding violations of liveness properties. The "PCT" scheduler of P# is unfair.

When using the portfolio scheduler, multiple schedulers are used, with an equal mix of fair and unfair schedulers. Because of their nature, unfair schedulers are expected to generate longer than usual executions. The unfairness in scheduling can lead to starvation of certain machine which may stall progress. The expected length of a program's execution is best determined by looking at lengths of "fair terminating executions", i.e., executions that terminate under a fair scheduler.

When a user supplies the flag `/max-steps:N`, executions under an unfair scheduler is forced to stop after N steps. Whereas, an execution under a fair scheduler can go to up 10*N steps. Further, if the execution stays in a hot state for more than 5*N steps, a liveness bug is flagged. The user can additionally supply the flag `/max-steps:N:M` to limit fair schedulers to explore only up to M steps (instead of 10*N).
