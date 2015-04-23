P#
====================
P# is a new language for high-reliability asynchronous .NET programming, *co-designed* with a static data race analysis and testing infrastructure. The co-design aspect of P# allows us to combine language design, analysis and testing in a unique way: the state-machine structure of a P# program enables us to create a more precise and scalable static analysis; while the race-freedom guarantees, provided by our analysis, contribute to the feasibility of systematically exploring a P# program to find bugs (e.g. assertion failures and unhandled exceptions).

## Build instructions
1. Get Visual Studio 2015 Preview (required for Microsoft Roslyn).
2. Clone this project and compile using VS2015.

## How to use
P# extends the C# language with state machines, states, state transitions and actions bindings. In P#, state machines are first class citizens and live in their own separate tasks. The only way they can communicate with each other is by explicitly sending and implicitly receiving events. As P# is based on C#, almost any valid C# code can be used in a P# method body (threading and code reflection APIs are not allowed).

The P# compiler can be used to parse a program, statically analyse it for data races and finally compile it to an executable. To invoke the compiler use the following command:

```
.\PSharpCompiler.exe /s:${PROJECT_PATH}\${SOLUTION_NAME}.sln
```

Where ${PROJECT\_PATH} is the path to your P# project and ${SOLUTION\_NAME} is the name of your P# solution.

To specify an output path destination please use the option ```/o:${OUTPUT\_PATH}```.

## Publications
- **Asynchronous Programming, Analysis and Testing with State Machines**. Pantazis Deligiannis, Alastair F. Donaldson, Jeroen Ketema, Akash Lal and Paul Thomson. In the *36th ACM SIGPLAN Conference on Programming Language Design and Implementation* (PLDI'15), 2015.
