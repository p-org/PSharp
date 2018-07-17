P# Race detector
================
The P# race detector detects data races that occur during the execution of a P# program. It intstruments all the memory accesses of the program under test and reports a pair of access as a race if 1) at least one of the accesses is a write and 2) the accesses are not causally ordered.

## Pre-Requisites
###Extended Reflection DLLs
The P# race detector depends on these DLLs to dynamically instrument the memory accesses of the program.
The race detector requires two DLLs 1)`Microsoft.ExtendedReflection.dll` and 2) `Microsoft.ExtendedReflection.ClrMonitor.X86.dll` which come with the CHESS tool that can be obtained [here](http://chesstool.codeplex.com/SourceControl/latest#ManagedChess/external/).

Place these DLLs in the directory  
`${PATH_TO_PSharp}\Source\AddOns\DynamicRaceDetection\ExtendedReflection`
where ${PATH_TO_PSharp} is the path to your PSharp directory.

## Building the race detector
```
cd ${PATH_TO_PSharp}\Source\AddOns\DynamicRaceDetection\
```
Open `DynamicRaceDetection.sln` with Visual studio in administrator mode and build it.

## Testing a P# program for races
```
cd ${PATH_TO_PSharp}\Source\Binaries
.\PSharpRaceDetector.exe /test:${DLL_PATH}\${DLL_NAME}.dll 
```
where ${DLL_PATH} is the path to your P# program and ${DLL_NAME} is the name of your P# program.

## Example
The example at Samples\Experimental\RaceTest contains a data race. 

Execute the following command from ${PATH_TO_PSharp}\Source:
```
.\Binaries\PSharpRaceDetector.exe /test:${PATH_TO_PSharpLab}\Samples\Experimental\RaceTest\RaceTest\bin\Debug\RaceTest.dll 
```
It produces the following output:
```
... Using 'Random' strategy.
..... Iteration #1
. Done
. Searching for data races

DETECTING RACES
RACE: D:\PSharpLab\Samples\Experimental\RaceTest\RaceTest\Server.cs;OnInint;21;write AND D:\PSharpLab\Samples\Experimental\RaceTest\RaceTest\Client.cs;OnPing;29;read
. Done
```

The line starting with 'RACE:' gives information about the racing accesses. It gives the name of the file in which the access occurs, its corresponding method name and the line number. It also indicates if the access was a read or a write.
