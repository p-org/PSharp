..\..\Binaries\PSharpCompiler.exe /s:Samples.sln /p:%1

..\..\..\chess\bin\mchess.exe /ia:Microsoft.PSharp Binaries\Debug\%1.exe /pct:10000:50 %2 %3 %4 %5 > out.txt
