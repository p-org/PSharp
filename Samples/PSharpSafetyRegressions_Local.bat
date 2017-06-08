@echo off
if not exist ..\..\PSharpBatchTesting\PSharpBatchTester\bin\Debug\PSharpBatchTester.exe (
	echo ERROR: Can't find PSharpBatchTester.exe
	exit
)
if not exist .\PSharpBatch.config (
	echo ERROR: Can't find .\PSharpBatch.config
	exit
)
..\..\PSharpBatchTesting\PSharpBatchTester\bin\Debug\PSharpBatchTester.exe /config:.\PSharpBatch.config /local


echo Test					Buggy schedules		Time taken	> .\PSharpSafetyResults.txt

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
	
	findstr "Testing" .\psharpbatchout.txt>z.txt
	set /p mystr3_%%s=<z.txt
	del z.txt
	for %%a in ("!mystr3_%%s!\.") do set "Bmk_%%s=%%~nxa"

	cd ..\..\
	echo !Bmk_%%s!				!Bug_%%s!		!Time_%%s!		>> .\PSharpSafetyResults.txt
	cd .\Output
)

::for /f "tokens=3" %%i in ("!mystr3_%%s!") do set Name_%%s=%%i
