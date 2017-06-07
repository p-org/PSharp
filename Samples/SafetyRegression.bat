..\..\PSharpBatchTesting\PSharpBatchTester\bin\Debug\PSharpBatchTester.exe /config:.\PSharpBatch.config /local

@echo off
echo Test		Buggy schedules		Time taken	> .\SafetyResults.txt

setlocal enabledelayedexpansion

cd .\Output
for /D %%s in (.\*) do (
	cd %%s
	findstr "buggy" .\psharpbatchout.txt>x.txt
	set /p mystr1_%%s=<x.txt
	for /f "tokens=3" %%i in ("!mystr1_%%s!") do set Bug_%%s=%%i

	findstr "Elapsed" .\psharpbatchout.txt>y.txt
	set /p mystr2_%%s=<y.txt
	for /f "tokens=3" %%i in ("!mystr2_%%s!") do set Time_%%s=%%i
	
	cd ..\..\
	echo %%s			!Bug_%%s!		!Time_%%s!		>> .\SafetyResults.txt
	cd .\Output
)
