P# Compiler
===========
The P# compiler can be used to parse a P# program, rewrite it to C# and finally compile it to an executable. To invoke the compiler use the following command:

```
.\PSharpCompiler.exe /s:${SOLUTION_PATH}\${SOLUTION_NAME}.sln
```

Where ${SOLUTION\_PATH} is the path to your P# solution and ${SOLUTION\_NAME} is the name of your P# solution.

To specify an output path destination use the option `/o:${OUTPUT\_PATH}`.

To compile only a specific project in the solution use the option `/p:${PROJECT_NAME}`.

To compile as a library (dll) use the option `/t:lib`.

To compile for testing use the option `/t:test`.

To see the available command line options use the option `/?`.

Alternatively, one can follow the instructions [here](../CodeEditors/VisualStudioLanguageSupport.md) to build directly from Visual Studio.
