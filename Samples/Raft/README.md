Raft
====
This is a single-process implementation of the [Raft](https://raft.github.io) consensus protocol written in P#.

The description of Raft can be found here: https://raft.github.io/raft.pdf

The aim of this sample is to showcase the testing capabilities of P#, and features such as nondeterministic timers and monitors (used to specify global safety and liveness properties).

## How to build
Open Raft.sln in Visual Studio and build.

## Injected bug

The sample contains a bug (injected on purpose), which is described [here](http://colin-scott.github.io/blog/2015/10/07/fuzzing-raft-for-fun-and-profit/) (see raft-45).
